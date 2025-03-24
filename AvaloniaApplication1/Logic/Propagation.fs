module AvaloniaApplication1.Logic.Propagation
open FSharp.Data.Adaptive
open AvaloniaApplication1.Models.Node

module Propagation =
    let input = cval 10.0

    let sumNode = AVal.map (fun x -> x + 5.0) input
    let multiplyNode = AVal.map (fun x -> x * 2.0) sumNode

    let output = multiplyNode

