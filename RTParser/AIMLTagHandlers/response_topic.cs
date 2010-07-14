﻿using System;
using System.Runtime;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using RTParser;
using RTParser.Utils;

namespace RTParser.AIMLTagHandlers
{
    public class response_topic : RTParser.Utils.AIMLTagHandler
    {

        public response_topic(RTParser.RTPBot bot,
                RTParser.User user,
                RTParser.Utils.SubQuery query,
                RTParser.Request request,
                RTParser.Result result,
                XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }



        protected override Unifiable ProcessChange()
        {

            if (this.templateNode.Name.ToLower() == "responsetopic")
            {
                try
                {
                    string payload = templateNodeInnerText.ToValue(query);
                    string payload2 = Recurse();
                    string payload3 = templateNode.InnerXml;
                    

                    string machine = GetAttribValue("machine", this.user.bot.pMSM.lastDefMachine);
                    string myState = GetAttribValue("state", this.user.bot.pMSM.lastDefState);
                    string myTopic = GetAttribValue("topic", null);
                    string prob_str = GetAttribValue("prob", "0.1");
                    double prob = double.Parse(prob_str);
                    payload= payload.Replace("rcategory", "category");
                    payload= payload.Replace("rpattern", "pattern");
                    string responseCode = "<aiml graph=\"msm\"> <topic name=\"" + myTopic + "\"> " + payload + " </topic> </aiml>";
                    RTPBot.writeDebugLine("MSM: response_topic ResponseCode = {0}", responseCode);
                    this.user.bot.AddAiml(responseCode);
                    // TODO: define machine-state -> topic
                    this.user.bot.pMSM.addResponse(machine, myState, myTopic, prob);

                }
                catch
                {
                }

            }
            return Unifiable.Empty;

        }
    }
}