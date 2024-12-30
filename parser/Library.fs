namespace parser

open System
open System.Text.RegularExpressions
open System.Globalization

type Header = string
type Event = string

type Message = {
    Timestamp: DateTime
    Nickname: string
    Content: string
}

module Parser =
    let parseHeader (line: string) : Header option =
        let regex = @"\[.*\]\ \[오[전후] \d{1,2}:\d{1,2}\]\ "
        let _match = Regex.Match(line, regex)
        if _match.Success then
            Some(_match.Value.Substring(0, _match.Length - 1))
        else
            None

    let parseNickname (header: Header) : string =
        let regex = @" \[오[전후] \d{1,2}:\d{1,2}\]"
        let parts = Regex.Split(header, regex)
        parts.[0].Substring(1, parts.[0].Length - 2)

    let parseTimestamp (header: Header) : string =
        let regex = @"\[오[전후] \d{1,2}:\d{1,2}\]"
        let _match = Regex.Match(header, regex)
        if _match.Success then
            _match.Value.Substring(1, _match.Length - 2)
        else
            ""

    let iterate (lines: seq<string>) : seq<obj> =
        let mutable currentDate = None
        let mutable buffer: obj option = None
    
        seq {
            for line in lines do
                if line.StartsWith("----") then
                    let koreanDateFormat = @"\d{4}년 \d{1,2}월 \d{1,2}일"
                    let dateMatch = Regex.Match(line, koreanDateFormat)
                    if dateMatch.Success then
                        let dateParts = Regex.Matches(dateMatch.Value, @"\d+")
                        let year = Int32.Parse(dateParts.[0].Value)
                        let month = Int32.Parse(dateParts.[1].Value)
                        let day = Int32.Parse(dateParts.[2].Value)
                        currentDate <- Some(DateTime(year, month, day))
                        match buffer with
                        | Some msg -> yield msg
                        | None -> ()
                        yield currentDate.Value
                else
                    let headerOpt = parseHeader(line)
                    match headerOpt with
                    | None when (line.EndsWith("님이 나갔습니다.") || line.EndsWith("부방장에서 해제되었습니다.") || line.EndsWith("들어왔습니다.")) ->
                        match buffer with
                        | Some msg -> yield msg
                        | None -> ()
                        yield Event(line) :> obj
                    | Some header ->
                        match buffer with
                        | Some msg -> yield msg
                        | None -> ()
                        let nickname = parseNickname(header)
                        let timestampStr = parseTimestamp(header).Replace("오전", "AM").Replace("오후", "PM")
                        let timestamp = DateTime.ParseExact($"{currentDate.Value.ToShortDateString()} {timestampStr}", "yyyy-MM-dd tt h:mm", CultureInfo.InvariantCulture)
                        let msgContent = line.Substring(header.Length + 1)
                        buffer <- Some({ Timestamp = timestamp; Nickname = nickname; Content = msgContent })
                    | None ->
                        match buffer with
                        | Some (:? Message as msg) ->
                            buffer <- Some({ msg with Content = msg.Content + "\n" + line })
                        | _ -> ()
            match buffer with
            | Some msg -> yield msg
            | None -> ()
        }