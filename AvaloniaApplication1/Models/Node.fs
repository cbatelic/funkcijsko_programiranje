module AvaloniaApplication1.Models.Node

open FSharp.Data.Adaptive

type NodeType =
    | Input
    | Sum
    | Multiply
    | Output

type Node = {
    Id: int
    Name: string
    NodeType: NodeType
    Inputs: int list
    Value: float option
}

type AdaptiveNode =
    { Id: int
      Name: string
      Value: cval<float>
      X: cval<float>
      Y: cval<float> }

type Connection =
    { FromNode: AdaptiveNode
      ToNode: AdaptiveNode }

