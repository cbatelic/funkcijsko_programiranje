namespace AvaloniaApplication1.Converters

open Avalonia.Data.Converters
open Avalonia.Media
open System
open System.Globalization

type BoolToBrushConverter() =
    interface IValueConverter with
        member _.Convert(value, targetType, parameter, culture) =
            match value with
            | :? bool as b when b -> SolidColorBrush(Colors.CadetBlue) :> obj
            | _ -> SolidColorBrush(Colors.Gray) :> obj

        member _.ConvertBack(_, _, _, _) = raise (NotImplementedException())
