open Fargo
open Fargo.Operators
open Pinicola.FSharp
open NugetHelper

type Command = | Paket

let parser =
    fargo {
        match! (cmd "paket" null "Add nuget package with paket") |>> Paket with
        | Paket -> return Paket
    }

FargoCmdLine.run
    "NugetHelper"
    parser
    (function
    | Paket -> PaketHelper.addNugetPackage ()
    )
