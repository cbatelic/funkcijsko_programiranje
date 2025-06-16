namespace AvaloniaApplication1.Converters

open Avalonia.Data.Converters
open Avalonia.Media
open System
open System.Globalization

type BoolToBrushConverter() =
    interface IValueConverter with
        member _.Convert(value, targetType, parameter, culture) =
            match value :?> bool with
            | true -> SolidColorBrush(Colors.DarkSlateBlue) :> obj
            | false -> SolidColorBrush(Colors.Transparent) :> obj

        member _.ConvertBack(value, targetType, parameter, culture) =
            raise (NotImplementedException())
