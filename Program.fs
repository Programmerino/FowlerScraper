// NEW

open FSharp.UMX
open FSharp.Text.RegexProvider
open FSharpPlus
open System.Reactive.Concurrency
open FSharp.Control.Reactive
open FSharp.Data

let DEBUG = true

[<Measure>]
type address

[<Measure>]
type availabledate

[<Measure>]
type bedrooms

[<Measure>]
type rent

[<Measure>]
type bathrooms

[<Measure>]
type squareft

[<Measure>]
type unitType

[<Measure>]
type url


type UnitDetails =
    Regex<"""href=\\"(?<url>[^"\\]+)[\S\s]*?h3\>(?<address>[^<]+)[\S\s]*?Type: <\/strong>(?<unitType>[^<]+)[\S\s]*?if\('(?<bedrooms>\d+)'[\S\s]+?Bathro{2}ms: <\/strong>(?<bathrooms>\d+)[\S\s]+?Monthly Rent: <\/strong>\$(?<rent>[\d,.]+)[\S\s]+?Square Fe{2}t: <\/strong>(?<squareft>\d+)[\S\s]+?Date Available: <\/strong>(?<availabledate>[\d/]+)""">


let urlMake x =
    let start = 10 * (x - 1)
    $"http://fow.ua.rentmanager.com/search_result?command=search_result&template=augustpreleasing&page={x}&start={start}&corpid=fow&mode=javascript&locations=1&orderby=aunituserdef_br,amarketrent"

type Details =
    { Address: string<address>
      AvailableDate: DateTime<availabledate>
      Bathrooms: int<bedrooms>
      SquareFeet: int<squareft>
      UnitType: string<unitType>
      Bedrooms: int<bedrooms>
      Rent: decimal<rent>
      Url: string<url> }

let log x = if DEBUG then printfn $"###{x}"

let logAndContinue x =
    x
    |> Observable.choose ((Result.mapError (tap log)) >> Option.ofResult)

[<EntryPoint>]
let main argv =

    let scheduler = NewThreadScheduler()

    [ 1 .. 10 ]
    |> Observable.ofSeqOn scheduler
    |> Observable.map urlMake
    |> Observable.flatmap (Http.AsyncRequestString >> Observable.ofAsync)
    |> Observable.flatmap (fun x -> UnitDetails().TypedMatches(x) |> Observable.ofSeq)
    |> Observable.map
        (fun details ->
            monad {
                let! datetime =
                    details.availabledate.Value
                    |> Result.protect System.DateTime.Parse

                return
                    { Address = %(details.address.Value |> String.replace "," "")
                      AvailableDate = %(datetime)
                      Bathrooms = %(details.bathrooms.Value |> int)
                      SquareFeet = %(details.squareft.Value |> int)
                      UnitType = %details.unitType.Value
                      Bedrooms = %(details.bedrooms.Value |> int)
                      Url = %details.url.Value
                      Rent =
                          %(details.rent.Value
                            |> String.replace "," ""
                            |> String.replace "$" ""
                            |> decimal) }
            })
    |> logAndContinue
    |> Observable.perform
        (fun { Address = address
               AvailableDate = availabledate
               Bathrooms = bathrooms
               Url = url
               Bedrooms = bedrooms
               Rent = rent
               SquareFeet = sqrft
               UnitType = unttyp } ->
            printfn $"http://fowlerrentals.com/{url},N/A,{bedrooms},{bathrooms},{address},N/A,N/A,N/A,{rent}")
    |> Observable.wait
    |> ignore

    0
