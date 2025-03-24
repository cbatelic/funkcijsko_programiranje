module AvaloniaApplication1.Models.Node

type NodeType =
    | Input of float
    | Output
    | Sum
    | Multiply
    | Split

type Node = {
    Id: string
    NodeType: NodeType
}


