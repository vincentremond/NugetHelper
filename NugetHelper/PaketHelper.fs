﻿namespace NugetHelper

open System
open System.IO
open Fake.Core
open Pinicola.FSharp.SpectreConsole
open FsToolkit.ErrorHandling
open Spectre.Console

module PaketHelper =

    module PaketCmd =

        let private execPaket command args =

            let arguments =
                Arguments.ofList (
                    [
                        "paket"
                        command
                    ]
                    @ args
                )

            let command = Command.RawCommand("dotnet", arguments)

            let processResult =
                CreateProcess.fromCommand command |> CreateProcess.redirectOutput |> Proc.run

            match processResult with
            | {
                  ExitCode = 0
                  Result = {
                               Output = output
                               Error = error
                           }
              } when String.isNullOrEmpty error && (not (output.Contains(" could not retrieve "))) ->
                output
                |> (fun s ->
                    s.Split(
                        [|
                            '\r'
                            '\n'
                        |],
                        StringSplitOptions.RemoveEmptyEntries
                    )
                    |> Array.toList
                )
            | _ ->
                failwithf
                    $"Paket command failed with exit code %d{processResult.ExitCode}\n{processResult.Result.Error}"

        let findPackages packageName =
            execPaket "find-packages" [
                packageName
                "--silent"
                // HACK (Paket issue #4243) : fails when using UNC path
                "--source"
                "https://api.nuget.org/v3/index.json"
            ]

        let findPackageVersions packageName =
            execPaket "find-package-versions" [
                packageName
                "--silent"
                // HACK (Paket issue #4243) : fails when using UNC path
                "--source"
                "https://packages.nuget.org/api/v2"
            ]

        let showGroups () = execPaket "show-groups" [ "--silent" ]

        let add packageId (version: string option) group project =
            AnsiConsole.status ()
            |> Status.run
                $"Adding [yellow]{packageId}[/] with version [yellow]{version}[/] in group [yellow]{group}[/] for project [yellow]{project}[/]"
                (fun _ ->
                    let output =
                        execPaket "add" [
                            yield packageId
                            match version with
                            | Some v ->
                                yield "--version"
                                yield v
                            | None -> ()
                            yield "--group"
                            yield group
                            yield "--project"
                            yield project
                        ]

                    output |> List.iter AnsiConsole.WriteLine
                )

    let private select what whatPlural search =

        let options: string list =
            AnsiConsole.status ()
            |> Status.start $"Getting {whatPlural}..." (fun _ -> search ())

        match options with
        | [] -> Error $"No {whatPlural} found"
        | [ option ] ->
            AnsiConsole.markupLine $"Found 1 {what}: [yellow]{option}[/]"
            Ok option

        | _ ->

            let selectedOption =
                SelectionPrompt.init ()
                |> SelectionPrompt.setTitle $"Found {options.Length} {whatPlural} :"
                |> SelectionPrompt.addChoices options
                |> AnsiConsole.prompt

            AnsiConsole.markupLine $"Selected {what}: [yellow]{selectedOption}[/]"

            Ok selectedOption

    let private searchProjects () =
        Directory.GetFiles(".", @"*.?sproj", SearchOption.AllDirectories)
        |> Seq.map (fun path -> Path.GetRelativePath(".", path))
        |> Seq.toList

    let addNugetPackage () =

        let selected =
            result {
                let packageName = AnsiConsole.ask "Enter the package name: "

                let! selectedPackage = select "package" "packages" (fun _ -> PaketCmd.findPackages packageName)

                // let! selectedVersion = select "version" "versions" (fun () -> PaketCmd.findPackageVersions selectedPackage)

                let! selectedGroup = select "group" "groups" PaketCmd.showGroups
                let! selectedProject = select "project" "projects" searchProjects

                return {|
                    PackageId = selectedPackage
                    Version = None
                    Group = selectedGroup
                    Project = selectedProject

                |}
            }

        match selected with
        | Ok s -> PaketCmd.add s.PackageId s.Version s.Group s.Project
        | Error err -> AnsiConsole.markupLine $"[red]{err}[/]"
