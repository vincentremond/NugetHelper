namespace NugetHelper

open System.Threading
open Fake.Core
open FsToolkit.ErrorHandling
open NuGet.Common
open NuGet.Configuration
open NuGet.Protocol
open NuGet.Protocol.Core.Types
open Pinicola.FSharp.SpectreConsole
open Spectre.Console

module NugetHelper =

    module NugetCmd =

        let findPackages (packageName: string) =
            task {
                let source = PackageSource("https://api.nuget.org/v3/index.json")
                let repository = Repository.Factory.GetCoreV3(source)
                let! searchResource = repository.GetResourceAsync<PackageSearchResource>()
                let searchFilter = SearchFilter(true)
                let! results = searchResource.SearchAsync(packageName, searchFilter, 0, 10, NullLogger.Instance, CancellationToken.None)
                return results |> Seq.map (_.Identity.Id) |> Seq.toList
            }

        let add (packageId: string) (project: string) =
            AnsiConsole.status ()
            |> Status.start
                $"Adding [yellow]{packageId}[/] for project [yellow]{project}[/]"
                (fun _ ->

                    let arguments =
                        Arguments.ofList [
                            "add"
                            project
                            "package"
                            packageId
                        ]

                    let command = Command.RawCommand("dotnet", arguments)

                    let processResult =
                        CreateProcess.fromCommand command |> CreateProcess.redirectOutput |> Proc.run

                    match processResult with
                    | {
                          ExitCode = 0
                          Result = { Output = output }
                      } -> Ok output
                    | {
                          ExitCode = _
                          Result = { Error = error }
                      } -> Error error
                )

    let addNugetPackage packageName exact =

        let selected =
            result {
                let! selectedPackage =
                    match exact with
                    | true -> Ok packageName
                    | false -> select "package" "packages" (fun _ -> (NugetCmd.findPackages packageName) |> Async.AwaitTask |> Async.RunSynchronously)

                let! selectedProject = select "project" "projects" searchProjects

                return {|
                    PackageId = selectedPackage
                    Project = selectedProject
                |}
            }

        let installResult =
            result {
                let! selected = selected

                let! installResult = NugetCmd.add selected.PackageId selected.Project
                AnsiConsole.markupLine $"Installed : [green]{selected.PackageId}[/]"
                AnsiConsole.WriteLine(installResult)
                return ()
            }

        match installResult with
        | Ok _ -> ()
        | Error err -> AnsiConsole.markupLine $"[red]{err}[/]"
