namespace NugetHelper

open System
open System.Diagnostics
open System.IO
open Fake.Core
open Fake.IO
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

            let process_ =
                CreateProcess.fromCommand command |> CreateProcess.redirectOutput |> Proc.run

            match process_.ExitCode with
            | 0 ->
                process_.Result.Output
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
            | _ -> failwithf $"Paket command failed with exit code %d{process_.ExitCode}\n{process_.Result.Error}"

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
                "https://api.nuget.org/v3/index.json"
            ]

        let showGroups () = execPaket "show-groups" [ "--silent" ]

        let add packageName version group project =
            execPaket "add" [
                packageName
                "--version"
                version
                "--group"
                group
                "--project"
                project
            ]
            |> ignore

    let addNugetPackage () =

        let packageName = AnsiConsole.Ask<string>("Enter the package name: ")

        let searchResults =
            AnsiConsole
                .Status()
                .Start(
                    $"Searching for package '[yellow]{packageName}[/]'...",
                    (fun context -> PaketCmd.findPackages packageName)
                )

        AnsiConsole.MarkupLine($"Found {searchResults.Length} packages")

        let selectedPackage =
            AnsiConsole.Prompt(SelectionPrompt<string>().AddChoices(searchResults))

        AnsiConsole.MarkupLine($"Selected package: [yellow]{selectedPackage}[/]")

        let versions =
            AnsiConsole
                .Status()
                .Start($"Getting package versions...", (fun context -> PaketCmd.findPackageVersions selectedPackage))

        AnsiConsole.MarkupLine($"Found {versions.Length} versions :")

        let selectedVersion =
            AnsiConsole.Prompt(SelectionPrompt<string>().AddChoices(versions))

        AnsiConsole.MarkupLine($"Selected version: [yellow]{selectedVersion}[/]")

        let groups =
            AnsiConsole
                .Status()
                .Start("Getting groups...", (fun context -> PaketCmd.showGroups ()))

        AnsiConsole.MarkupLine($"Found {groups.Length} groups :")
        let selectedGroup = AnsiConsole.Prompt(SelectionPrompt<string>().AddChoices(groups))

        let projects =
            AnsiConsole
                .Status()
                .Start(
                    $"Getting projects...",
                    (fun context ->
                        Directory.GetFiles(".", "*.?sproj", SearchOption.AllDirectories)
                        |> Seq.map (fun path -> Path.GetRelativePath(".", path))
                        |> Seq.toList
                    )
                )

        AnsiConsole.MarkupLine($"Found {projects.Length} projects :")

        let selectedProject =
            AnsiConsole.Prompt(SelectionPrompt<string>().AddChoices(projects))

        PaketCmd.add selectedPackage selectedVersion selectedGroup selectedProject

        ()
