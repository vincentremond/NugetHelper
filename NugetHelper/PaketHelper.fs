namespace NugetHelper

open System
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

        let private execPaketWithOutput command args onStdOut onStdErr =

            let arguments =
                Arguments.ofList (
                    [
                        "paket"
                        command
                    ]
                    @ args
                )

            let command = Command.RawCommand("dotnet", arguments)

            CreateProcess.fromCommand command
            |> CreateProcess.redirectOutput
            |> CreateProcess.withOutputEvents onStdOut onStdErr
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

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

        let add packageId group project =
            AnsiConsole.status ()
            |> Status.run
                $"Adding [yellow]{packageId}[/] in group [yellow]{group}[/] for project [yellow]{project}[/]"
                (fun _ ->

                    let ruleStyle =
                        StyleBuilder.default_
                        |> StyleBuilder.withForeground (Color.Grey)
                        |> StyleBuilder.build

                    let rule = Rule.init () |> Rule.setStyle ruleStyle

                    AnsiConsole.Write rule

                    execPaketWithOutput
                        "add"
                        [
                            packageId
                            "--group"
                            group
                            "--project"
                            project
                        ]
                        (writeToConsole "grey")
                        (writeToConsole "red")

                    AnsiConsole.Write rule

                )

    let addNugetPackage packageName exact =

        let selected =
            result {

                let! selectedPackage =
                    match exact with
                    | true -> Ok packageName
                    | false -> select "package" "packages" (fun _ -> PaketCmd.findPackages packageName)

                let! selectedGroup = select "group" "groups" PaketCmd.showGroups
                let! selectedProject = select "project" "projects" searchProjects

                return {|
                    PackageId = selectedPackage
                    Group = selectedGroup
                    Project = selectedProject
                |}
            }

        match selected with
        | Ok s -> PaketCmd.add s.PackageId s.Group s.Project
        | Error err -> AnsiConsole.markupLine $"[red]{err}[/]"
