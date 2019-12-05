module Parsec

type Location =
    { line: int
      chr: int }

    override this.ToString () =
        string(this.line + 1) + ":" + string(this.chr + 1)

    static member (+) (lhs : Location, rhs : Location) =
        {
            line = lhs.line + rhs.line
            chr = if rhs.line >= 1 then rhs.chr else lhs.chr + rhs.chr
        }

let bof = { line = 0; chr = 0 }

type Success<'ast> =
    { ast : 'ast; currentLoc : Location; rest : string }

type Parser<'ast> = Location * string -> Option<'ast Success>

let fmap (f : 'a -> 'b) (p : Parser<'a>) input =
    p input
    |> Option.map (fun result ->
        {
            ast = f result.ast
            currentLoc = result.currentLoc
            rest = result.rest
        })

let (<*>) (precede : Parser<'a -> 'b>) (succeed : Parser<'a>) input =
    precede input
    |> Option.map
        (fun { ast = f; currentLoc = precedeLoc; rest = rest} ->
            (fmap f succeed) (precedeLoc, rest))

let (<|>) (p : Parser<'a>) (q : Parser<'a>) input =
    Option.orElseWith (fun () -> q input) (p input)

let (|=) (p : Parser<'a>) (f : 'a -> Parser<'b>) input =
    p input
    |> Option.bind (fun { ast = ast; currentLoc = loc; rest = rest } ->
        (f ast) (loc, rest))

let (|.) m f = m |= fun _ -> f

open System

let token (tok : string) (loc, src : string) =
    let br = Environment.NewLine
    if src.StartsWith tok
        then
            let lines = tok.Split ([|br|], StringSplitOptions.RemoveEmptyEntries)
            let length = String.length tok
            Some {
                ast = (loc, tok)
                currentLoc =
                    loc + {
                            line = Array.length lines - 1
                            chr = String.length <| Array.last lines
                        }
                rest = src.Substring (length, (String.length src - length))
            }
        else None
