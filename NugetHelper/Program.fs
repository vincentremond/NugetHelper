open Fargo
open Fargo.Operators
open NugetHelper
open Pinicola.FSharp
open Pinicola.FSharp.SpectreConsole

type Command =
    | Paket
    | Nuget

let parser =
    fargo {

        let! command =
            ((cmd "paket" null "Add nuget package with paket") |>> Paket)
            <|> ((cmd "nuget" null "Add nuget package with nuget") |>> Nuget)

        let! packageId = arg "packageId" "Package id"
        let! exact = flag "exact" "e" "Exact package name"
        return command, packageId, exact
    }

FargoCmdLine.run
    "NugetHelper"
    parser
    (fun (command, packageId, exact) ->

        let packageName =
            match packageId with
            | Some packageId -> packageId
            | None -> AnsiConsole.ask "Enter the package name: "

        let add =
            match command with
            | Paket -> PaketHelper.addNugetPackage
            | Nuget -> NugetHelper.addNugetPackage

        add packageName exact
    )
