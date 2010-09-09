using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using LAIR.ResourceAPIs.WordNet;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using MushDLR223.Virtualization;
using RTParser.AIMLTagHandlers;
using RTParser.Database;
using RTParser.Normalize;
using RTParser.Utils;
using RTParser.Variables;
using UPath = RTParser.Unifiable;
using UList = System.Collections.Generic.List<RTParser.Utils.TemplateInfo>;


namespace RTParser
{
    public partial class RTPBot
    {

        public void HeardSelfSayVerbal(string message, Result result)
        {
            currentEar.AddMore(message);
            if (!currentEar.IsReady())
            {
                return;
            }
            botJustSaid = message;
            message = currentEar.GetMessage();
            currentEar = new JoinedTextBuffer();
            result = result ?? (LastUser == null ? null : LastUser.LastResult);
            ManualResetEvent newManualResetEvent = new ManualResetEvent(false);
            ThreadControl control = new ThreadControl(newManualResetEvent);
            HeardSelfSay1Sentence(message, result, control);
        }

        public Result HeardSelfSayResponse(string message, Result result, ThreadControl control)
        {
            Result LR = null;
            if (message == null) return result;
            bool lts = ListeningToSelf;
            bool prochp = ProcessHeardPreds;
            if (!lts && !prochp) return result;
            message = ToHeard(message);
            if (message == null) return result;
            
            bool stopProcessing = false;
            if (control != null) control.AbortOrInteruptedRaised += (ctl, ex) => { stopProcessing = true; };

            int index = message.IndexOfAny("!.?".ToCharArray());
            if (index > 0)
            {
                string message1 = message.Substring(0, index);
                result = HeardSelfSay1Sentence(message1, result, control);
                message = message.Substring(index);
                if (stopProcessing) return AbortedResult(message, result, control);
                return HeardSelfSayResponse(message, result, control);
            }
            if (stopProcessing) return AbortedResult(message, result, control);
            return HeardSelfSay1Sentence(message, result, control);
        }

        internal Result AbortedResult(string message, Result result, ThreadControl control)
        {
            return result;
        }

        public Result HeardSelfSay1Sentence(string message, Result result, ThreadControl control)
        {
            Result LR = result;
            if (message == null) return LR;

            bool lts = ListeningToSelf;
            bool prochp = ProcessHeardPreds;
            if (!lts && !prochp) return LR;

            message = ToHeard(message);
            if (message == null) return LR;
            //message = swapPerson(message);
            //writeDebugLine("HEARDSELF SWAP: " + message);

            if (prochp)
            {
                writeDebugLine("-----------------------------------------------------------------");
                AddHeardPreds(message, HeardPredicates);
                writeDebugLine("-----------------------------------------------------------------");
            }
            if (!lts)
            {
                writeDebugLine("-----------------------------------------------------------------");
                writeDebugLine("SELF: " + message);
                writeDebugLine("-----------------------------------------------------------------");
                return null;
            }
            bool wasQuestion = WasQuestion(message);
            string desc = string.Format("ROBOT {1} USER: {0}", message, wasQuestion ? "ASKS" : "TELLS");
            return RememberSpoken(desc, message, result, control);
        }

        private static bool WasQuestion(string message)
        {
            return message.Contains("?");
        }

        private Result RememberSpoken(string desc, string message, Result result, ThreadControl control)
        {
            writeChatTrace(desc);
            if (control == null)
            {
                return UnderstandSentence(desc, message, result, control);
            }

            control.AbortRaised += (ctl, abrtedE) => writeToLog("Aborted " + desc);
            HeardSelfSayQueue.EnqueueWithTimeLimit(()
                                                   => result = UnderstandSentence(desc, message, result, control), desc,
                                                   TimeSpan.FromSeconds(10), control);
            control.WaitUntilComplete();
            return result;
        }


        private Result UnderstandSentence(string desc, string message, Result res, ThreadControl control)
        {
            Result LR = res;
            if (LR != null && LastUser != null)
            {
                lock (LastUser.QueryLock)
                {
                    LR = LastUser.LastResult;
                    if (LR != null)
                    {
                        LR.AddOutputSentences(null, message);
                    }
                }
            }
            writeDebugLine("-----------------------------------------------------------------");
            writeDebugLine("HEARDSELF: " + message);
            writeDebugLine("-----------------------------------------------------------------");
            return LR;
            try
            {
                if (message == null || message.Length < 4) return null;
                try
                {
                    Request r = new AIMLbot.Request(message, BotAsUser, this, null);
                    r.writeToLog = writeDebugLine;
                    r.IsTraced = false;
                    return ChatWithUser(r, BotAsUser, HeardSelfSayGraph);
                }
                catch (Exception e)
                {
                    writeToLog(e);
                    return null;
                }
            }
            finally
            {
                BotAsUser.Predicates.addSetting("lastsaid", message);
            }
        }

        private void AddHeardPreds(string message, SettingsDictionary dictionary)
        {
            if (message == null) return;
            writeDebugLine("-----------------------------------------------------------------");
            foreach (string s in message.Trim().Split(new[] { '!', '?', '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                AddHeardPreds0(s, dictionary);
            }
            writeDebugLine("-----------------------------------------------------------------");
            //RTPBot.writeDebugLine("" + dictionary.ToDebugString());
        }

        private void AddHeardPreds0(Unifiable unifiable, SettingsDictionary dictionary)
        {
            if (unifiable.IsEmpty) return;
            Unifiable first = unifiable.First();
            if (first.IsEmpty) return;
            Unifiable rest = unifiable.Rest();
            if (rest.IsEmpty) return;
            dictionary.addSetting(first, rest);
            AddHeardPreds0(rest, dictionary);
        }
    }
}