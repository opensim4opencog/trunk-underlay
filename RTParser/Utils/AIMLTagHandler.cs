using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using MushDLR223.Utilities;
using RTParser.AIMLTagHandlers;
using RTParser.Database;
using RTParser.Variables;
using LineInfoElement = MushDLR223.Utilities.LineInfoElementImpl;

namespace RTParser.Utils
{
    /// <summary>
    /// The template for all classes that handle the AIML tags found within template nodes of a
    /// category.
    /// </summary>
    public abstract partial class AIMLTagHandler : TextTransformer, IXmlLineInfo, IDisposable
    {
        /// <summary>
        /// A representation of the input into the Proc made by the user
        /// </summary>
        public Request _request;

        /// <summary>
        /// A representation of the result to be returned to the user
        /// </summary>
        private Result _result0;

        /// <summary>
        /// A flag to denote if inner tags are to be processed recursively before processing this tag
        /// </summary>
        public bool isRecursive = true;

        public bool IsStarAtomically = false; // true break it right now
        public bool IsStarted;
        public bool IsOverBudget;
        public bool IsDisposing;
        public AIMLTagHandler Parent;

        /// <summary>
        /// The query that produced this node containing the wildcard matches
        /// </summary>
        public SubQuery query;

        internal TemplateInfo templateInfo;

        /// <summary>
        /// The template node to be processed by the class
        /// </summary>
        public XmlNode templateNode;

        /// <summary>
        /// A representation of the user who made the request
        /// </summary>
        public User user;

        /// <summary>
        /// Default ctor to use when late binding
        /// </summary>
        public AIMLTagHandler()
        {
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request itself</param>
        /// <param name="result">The result to be passed back to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public AIMLTagHandler(RTPBot bot,
                              User user,
                              SubQuery query,
                              Request request,
                              Result result,
                              XmlNode templateNode)
            : base(bot, templateNode.OuterXml)
        {
            this.user = user;
            this.query = query;
            this._request = request;
            this.result = result;
            this.templateNode = templateNode;
            inputString = templateNode.OuterXml;
            initialString = inputString;
            if (this.templateNode.Attributes != null) this.templateNode.Attributes.RemoveNamedItem("xmlns");
        }

        public SubQuery TheQuery
        {
            get
            {
                SubQuery ret = this.query
                    //?? request.CurrentQuery ?? this.query ?? result.CurrentQuery
                    ;
                return ret;
            }
        }

        public Request request
        {
            get
            {
                if (query != null)
                {
                    if (_request == query.Request)
                    {
                        return _request;
                    }
                    return query.Request;
                }
                return _request;
            }
            set { _request = value; }
        }

        public Result result
        {
            get
            {
                if (query != null)
                {
                    if (_result0 == query.Result)
                    {
                        return _result0;
                    }
                    else
                    {
                        return _result0;
                    }
                    return query.Result;
                }
                return _result0;
            }
            set { _result0 = value; }
        }

        protected bool ReadOnly;

        protected RTPBot TargetBot
        {
            get
            {
                if (query != null) return query.TargetBot;
                if (result != null) return result.TargetBot;
                if (request != null) return request.TargetBot;
                if (user != null) return user.bot;
                return Proc;
            }
        }

        public string GetTemplateNodeInnerText()
        {
            return templateNodeInnerText;
        }

        protected Unifiable templateNodeInnerText
        {
            get
            {
                string s2 = null;
                if (!innerResult.IsValid)
                {
                    s2 = Recurse();
                }
                if (innerResult.IsValid)
                {
                    s2 = innerResult.Value;
                }
                else if (finalResult.IsValid) return finalResult.Value;
                string sr = s2 ?? InnerXmlText(templateNode);
                sr = ValueText(sr);
                return CheckValue(sr);
            }

            set
            {
                if (Unifiable.IsNullOrEmpty(value))
                {
                    writeToLogWarn("ERROR ?!?! templateNodeInnerText = " + value);
                }
                if (InUnify)
                {
                    writeToLogWarn("InUnify ?!?! templateNodeInnerText = " + value);
                    return;
                }
                string valueAsString = value.AsString();

                bool isOuter = valueAsString.StartsWith(isValueSetStart);
                if (isOuter)
                {
                    RecurseResult = ValueText(valueAsString);
                    //innerResult.Value = value;
                }
                else
                {
                    innerResult.Value = value;
                }

                if (!valueAsString.Contains("<a href"))
                    if (ContainsAiml(valueAsString))
                    {
                        writeToLogWarn("ContainsAiml = " + valueAsString);
                    }
                templateNode.InnerText = CheckValue(value);
            }
        }

        public virtual bool QueryHasFailed
        {
            get { return query.HasFailed > 0; }
            set
            {
                if (InUnify)
                {
                    writeToLog("InUnify QueryHasFailed=" + value);
                    return;
                }
                query.HasFailed += (value ? 1 : 0);
            }
        }
        public virtual bool QueryHasSuceeded
        {
            get { return query.HasSuceeded > 0; }
            set
            {
                if (InUnify)
                {
                    writeToLog("InUnify QueryHasFailed=" + value);
                    return;
                }
                query.HasSuceeded += (value ? 1 : 0);
            }
        }

        public virtual int QueryHasFailedN
        {
            get { return query.HasFailed; }
            set { query.HasFailed = value; }
        }
        public virtual int QueryHasSuceededN
        {
            get { return query.HasSuceeded; }
            set { query.HasSuceeded = value; }
        }
        protected bool InUnify;

        protected ResultCache finalResult = new ResultCache();
        protected ResultCache innerResult = new ResultCache();
        private ICollection<AIMLTagHandler> TagHandlerChilds;
        public static bool SaveAIMLChange = true;
        public static bool SaveAIMLComplete = false;
        public static bool EnforceStartedBool = false;

        public bool RecurseResultValid
        {
            get
            {
                string templateNodeInnerXml = templateNode.InnerXml;
                if (templateNodeInnerXml.StartsWith(isValueSetStart))
                {
                    if (templateNodeInnerXml.Length > 3)
                    {
                        return true;
                    }
                    if (!finalResult.IsValid)
                    {
                        return false;
                    }
                }
                if (!finalResult.IsValid)
                {
                    return false;
                }
                var recurseResult = this.RecurseResult;
                {
                    if (IsNull(recurseResult))
                    {
                        writeToLogWarn("IsNull _RecurseResult to String.Empty at least!");
                    }
                    else if (IsMissing(recurseResult))
                    {
                        writeToLogWarn("IsMissing _RecurseResult to String.Empty at least!");
                    }
                }
                return finalResult.IsValid;
            }
            set
            {
                var recurseResult = this.RecurseResult;
                if (value)
                {
                    if (IsNull(recurseResult))
                    {
                        writeToLogWarn("IsNull _RecurseResult to String.Empty at least!");
                    }
                    else
                        if (IsMissing(recurseResult))
                        {
                            writeToLogWarn("IsMissing _RecurseResult to String.Empty at least!");
                        }
                }
                finalResult.IsValid = value;
            }
        }

        /// <summary>
        /// Final Result (not "innerResult!")
        /// </summary>
        public Unifiable RecurseResult
        {
            get
            {
                if (!finalResult.IsValid)
                {
                    var vv = ToXmlValue(templateNode);
                    if (templateNode.InnerXml.StartsWith(isValueSetStart))
                    {
                        return CheckValue(vv);
                    }
                    return null;
                }
                return finalResult.Value;
            }
            set
            {
                if (!IsNullOrEmpty(value))
                {
                    string xmlValueSettable = XmlValueSettable(value); ;
                    templateNode.InnerXml = xmlValueSettable;
                    var vv = CheckValue(value);
                    finalResult.Value = vv;
                    if (InUnify)
                    {
                        return;
                    }
                }
                if (IsNull(value))
                {
                    writeToLog("WARN: Null = " + null);
                    return;
                }
                if (IsEMPTY(value))
                {
                    writeToLog("WARN: EXPTY" + null);
                }
            }
        }

        public void SetParent(AIMLTagHandler handler)
        {
            if (handler == this)
            {
                throw new InvalidOperationException("same");
            }
            else if (handler == null)
            {
                //throw new InvalidOperationException("no parent handler");                
            }
            else
            {
                if (handler.InUnify)
                {
                    InUnify = handler.InUnify;
                }
                else
                {
                    InUnify = handler.InUnify;
                }
            }
            Parent = handler;
            ResetValues(true);
        }

        public void ResetValues(bool childsTo)
        {
            QueryHasFailed = false;
            IsStarted = false;
            finalResult.Reset();
            innerResult.Reset(); // Unifiable.NULL;
        }

        // This was from the original source of the AIML Tag

        public TemplateInfo GetTemplateInfo()
        {
            if (templateInfo == null)
            {
                if (query != null)
                {
                    templateInfo = query.CurrentTemplate;
                }
                if (templateInfo != null) return templateInfo;
                if (Parent != null)
                {
                    templateInfo = Parent.GetTemplateInfo();
                    if (templateInfo != null) return templateInfo;
                }
            }
            return templateInfo;
        }

        /// <summary>
        /// By calling this and not just ProcessChange() 
        /// You've ensure we have a proper calling context
        /// </summary>
        /// <returns></returns>
        public override Unifiable ProcessAimlChange()
        {
            if (finalResult.IsValid) return finalResult.Value;
            ThreadStart OnExit = EnterTag(request, templateNode, query);
            try
            {
                if (RecurseResultValid) return RecurseResult;
                IsStarted = true;
                var recurseResult = ProcessChange();
                if (!Unifiable.IsNull(recurseResult))
                {
                    if (CheckNoZAIMLTodo(recurseResult))
                    {
                        recurseResult = ProcessAimlTemplate(recurseResult);
                    }
                    RecurseResult = recurseResult;
                    return  recurseResult;
                }
                recurseResult = RecurseProcess();
                if (!Unifiable.IsNull(recurseResult))
                {
                    if (CheckNoZAIMLTodo(recurseResult))
                    {
                        recurseResult = ProcessAimlTemplate(recurseResult);
                    }
                    RecurseResult = recurseResult;
                    return recurseResult;
                }
                var recurseResult1 = templateNodeInnerText;
                if (!Unifiable.IsNull(recurseResult1))
                {
                    if (CheckNoZAIMLTodo(recurseResult1))
                    {
                        recurseResult1 = ProcessAimlTemplate(recurseResult1);

                    }
                    RecurseResult = recurseResult1;
                    return recurseResult1;
                }
                if (CheckNoZAIMLTodo(recurseResult))
                {
                    recurseResult = ProcessAimlTemplate(recurseResult);
                }
                return recurseResult;
            }
            finally
            {
                if (OnExit != null) OnExit();
            }
        }

        /// <summary>
        /// By calling this and not just CompleteProcess() 
        /// You've ensure we have a proper calling context
        /// </summary>
        public Unifiable CompleteAimlProcess()
        {
            if (finalResult.IsValid) return finalResult.Value;
            if (RecurseResultValid) return RecurseResult;
            ThreadStart OnExit = EnterTag(request, templateNode, query);
            try
            {
                if (RecurseResultValid) return RecurseResult;
                Unifiable test = CompleteProcess();
                if (Unifiable.IsNull(test))
                {
                    writeToLogWarn("NULL " + test);
                }
                if (Unifiable.IsNull(RecurseResult))
                {
                    //writeToLog("NULL RecurseResult");
                }
                if (test == RecurseResult) return test;
                if (Unifiable.IsNull(test))
                {
                    test = GetTemplateNodeInnerText();
                    return test;
                }
                return test;
            }
            finally
            {
                if (OnExit != null) OnExit();
            }
        }

        /// <summary>
        /// Helper method that turns the passed Unifiable into an XML node
        /// </summary>
        /// <param name="outerXML">the Unifiable to XMLize</param>
        /// <returns>The XML node</returns>
        public virtual float CanUnify(Unifiable with)
        {
            string w = with.ToValue(query);
            Unifiable t1 = ProcessAimlChange();
            float score1 = t1.Unify(with, query);
            if (score1 == 0) return score1;
            Unifiable t2 = CompleteAimlProcess();
            if (ReferenceEquals(t1, t2)) return score1;
            float score2 = t2.Unify(with, query);
            if (score2 == 0) return score2;
            return (score1 < score2) ? score1 : score2;
        }


        public ThreadStart EnterUnify()
        {
            bool prev = NamedValuesFromSettings.UseLuceneForGet;
            bool prevUnify = InUnify;
            NamedValuesFromSettings.UseLuceneForGet = false;
            InUnify = true;
            return () =>
                       {
                           InUnify = prevUnify;
                           NamedValuesFromSettings.UseLuceneForGet = prev;
                       };
        }

        public override sealed float CallCanUnify(Unifiable with)
        {
            var exitUnify = EnterUnify();
            try
            {
                return CanUnify(with);
            }
            finally
            {
                exitUnify();
            }
        }

        protected Unifiable Failure(string p)
        {
            writeToLog("<!-- FAILURE: " + p.Replace("<!--", "<#-").Replace("-->", "-#>") + "-->");
            return Unifiable.Empty;
            this.QueryHasFailedN++;
        }

        protected Unifiable Succeed(string p)
        {
            this.QueryHasSuceededN++;
            return "<!-- SUCCEED: " + p.Replace("<!--", "<#-").Replace("-->", "-#>") + "-->";
        }

        protected Unifiable Succeed()
        {
            if (query != null && query.CurrentTemplate != null)
            {
                string type = GetType().Name;
                double defualtReward = query.GetSucceedReward(type);
                double score = GetAttribValue<double>(templateNode, "score", () => defualtReward,
                                                      ReduceStarAttribute<double>);
                writeToLog("TSCORE {3} {0}*{1}->{2} ",
                           score, query.CurrentTemplate.Rating,
                           query.CurrentTemplate.Rating *= score, score);
            }
            string s = Unifiable.InnerXmlText(templateNode);
            if (string.IsNullOrEmpty(s))
            {
                if (RecurseResultValid) return RecurseResult;
                return Succeed(templateNode.OuterXml);
            }
            return s;
        }

        protected T ReduceStarAttribute<T>(IConvertible arg) where T : IConvertible
        {
            bool found;
            return ReduceStar<T>(arg, query, query, out found);
        }

        protected Unifiable ReduceStarAttribute(IConvertible arg)
        {
            bool found;
            return ReduceStar<Unifiable>(arg, query, query, out found);
        }

        public bool WhenTrue(Unifiable unifiable)
        {
            if (!Unifiable.IsNullOrEmpty(unifiable))
            {
                if (Unifiable.IsFalse(unifiable)) return false;
                if (!Unifiable.IsTrue(unifiable))
                {
                    writeToLog("DEBUG: !WhenTrue " + unifiable);
                }
                //templateNodeInnerText = isValueSetStart + unifiable;                
                RecurseResult = unifiable;
                return true;
            }
            return false;
        }

        public Unifiable GetStarContent()
        {
            XmlNode starNode = getNode("<star/>", templateNode);
            LineInfoElement.unsetReadonly(starNode);
            star recursiveStar = new star(this.Proc, this.user, this.query, this.request, this.result, starNode);
            return recursiveStar.Transform();
        }

        protected Unifiable callSRAI(string starContent)
        {
            if (RecurseResultValid)
            {
                return RecurseResult;
            }
            XmlNode sraiNode = getNode(String.Format("<srai>{0}</srai>", starContent), templateNode);
            LineInfoElement.unsetReadonly(sraiNode);
            srai sraiHandler = new srai(this.Proc, this.user, this.query, this.request, this.result, sraiNode);
            var vv = sraiHandler.Transform();
            RecurseResult = vv;
            if (!Unifiable.IsNull(vv))
            {
                return vv;
            }
            return vv;
        }

        /// <summary>
        /// The method that does the recursion of the text
        /// </summary>
        /// <returns>The resulting processed text</returns>
        protected Unifiable TransformAtomically(Func<Unifiable, Unifiable>
                                                    afterEachOrNull, bool saveResultsOnChildren)
        {
            afterEachOrNull = afterEachOrNull ?? ((passthru) => passthru);
            bool templateNodeHasChildNodes = templateNode.HasChildNodes;
            // pre textualized ?
            var innerText = this.templateNodeInnerText;
            if (!templateNodeHasChildNodes && IsStarAtomically)
            {
                // atomic version of the node
                Unifiable templateResult = GetStarContent();

                Unifiable a = afterEachOrNull(templateResult);
                if (saveResultsOnChildren)
                {
                    SaveResultOnChild(templateNode, a);
                    return a;
                }
                return a;
            }

            {
                {
                    // needs some recursion
                    StringAppendableUnifiableImpl templateResult = Unifiable.CreateAppendable();
                    foreach (XmlNode childNode in templateNode.ChildNodes)
                    {
                        try
                        {
                            bool protectChildren = ReadOnly;

                            Unifiable part = ProcessChildNode(childNode);

                            part = afterEachOrNull(part);
                            if (saveResultsOnChildren)
                            {
                                SaveResultOnChild(childNode, part);
                            }
                            templateResult.Append(part);
                        }
                        catch (ChatSignal ex)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            RTPBot.writeDebugLine("ERROR: {0}", e);
                        }
                    }
                    return templateResult;
                }
            }
            return Unifiable.Empty;
        }

        public string GetAttribValue(string attribName, string defaultIfEmpty)
        {
            return GetAttribValue(templateNode, attribName, () => defaultIfEmpty, query.ReduceStarAttribute<string>);
        }

        public virtual Unifiable CheckValue(Unifiable value)
        {
            if (ReferenceEquals(value, Unifiable.Empty)) return value;
            if (value == null)
            {
                writeToLogWarn("ChackValue NULL");
                return null;
            }
            else
            {
                if (Unifiable.IsNull(value))
                {
                    writeToLogWarn("CheckValue NULL = '" + Unifiable.DescribeUnifiable(value) + "'");
                    return value;
                }
                if (Unifiable.IsEMPTY(value))
                {
                    //writeToLogWarn("CheckValue EMPTY = '" + value + "'");
                    return value;
                }
                if (!CheckNoZAIMLTodo(value))
                {
                    return value;
                }
                var recurseResult = ProcessAimlTemplate(value);
               // writeToLogWarn("CheckValue XML = '" + (string)value + "'");
                return value;
            }
        }

        protected bool CheckNoZAIMLTodo(Unifiable value)
        {
            string v = (string)value;
            if (value == null) return false;
            if ((!v.Contains("<a href") && !v.Contains("<!--")))
            {
                if (v.Contains("<"))
                {
                    return true;
                }
                else if (v.Contains("&"))
                {
                    return true;
                }
            }
            return false;
        }

        protected AIMLTagHandler GetChildTagHandler(XmlNode childNode)
        {
            AIMLTagHandler part = Proc.GetTagHandler(user, query, request, result, childNode, this);
            AddChild(part);
            return part;
        }


        protected Unifiable ProcessChildNode(XmlNode childNode)
        {
            bool success;
            var chosenXML = Unifiable.InnerXmlText(childNode);
            var vv = ProcessChildNode(childNode, ReadOnly, false, out success);
            if (!success)
            {
                QueryHasFailedN++;

                writeToLogWarn("EVALING CHILD " + vv + " " + childNode);
                vv = ProcessChildNode(childNode, ReadOnly, false, out success);
            }
            if (!IsNullOrEmpty(vv))
            {
                return vv;
            }
            if (Unifiable.IsNullOrEmpty(vv))
            {
                var vv2 = GetTemplateNodeInnerText();
                return vv;
            }
            return vv;
        }

        protected Unifiable ProcessChildNode(XmlNode childNode, bool protectChildren, bool saveOnInnerXML, out bool success)
        {
            var vv = ProcessChildNode00(childNode, protectChildren, saveOnInnerXML, out success);
            if (!success)
            {
                QueryHasFailedN++;
            }
            if (success && ContainsAiml(vv))
            {
                vv = ProcessAimlTemplate(vv);
            }
            if (!IsNullOrEmpty(vv) || childNode.LocalName=="think")
            {
                return vv;
            }
            return vv;
        }

        private Unifiable ProcessAimlTemplate(Unifiable vv)
        {
            bool success;
            var vv2 = ProcessChildNode00(StaticAIMLUtils.getTemplateNode(vv), false, false, out success);
            writeToLog("Continue sub-Processing of " + vv + " -> " + vv2);
            if (success) vv = vv2;
            if (!IsNullOrEmpty(vv))
            {
                return vv;
            }
            return vv;
        }

        protected Unifiable ProcessChildNode00(XmlNode childNode, bool protectChildren, bool saveOnInnerXML, out bool success)
        {
            if (saveOnInnerXML) throw new InvalidOperationException("saveOnInnerXML!" + this);
            try
            {
                string childNodeInnerXml = childNode.InnerXml;
                if (childNodeInnerXml.StartsWith(isValueSetStart))
                {
                    string s = ValueText(childNodeInnerXml);
                    success = true;
                    return s;
                }
                if (childNode.NodeType == XmlNodeType.Text)
                {
                    string value = childNode.InnerText.Trim();

                    if (value.StartsWith(isValueSetStart))
                    {
                        success = true;
                        return ValueText(value);
                    }
                    if (saveOnInnerXML)
                    {
                        childNode.InnerText = XmlValueSettable(value);
                    }
                    success = true;
                    return value;
                }
                else if (childNode.NodeType == XmlNodeType.Comment)
                {
                    success = true;
                    if (saveOnInnerXML) childNode.InnerXml = XmlValueSettable("");
                    return String.Empty;
                }
                else
                {
                    bool copyParent, copyChild;
                    copyParent = copyChild = protectChildren;

                    AIMLTagHandler tagHandlerChild;
                    string value = Proc.processNode(childNode, query,
                                                    request, result, user,
                                                    this, copyChild, copyParent,
                                                    out tagHandlerChild);
                    success = true;
                    if (IsNull(value))
                    {
                        if (tagHandlerChild != null && tagHandlerChild.QueryHasSuceeded)
                            success = true;
                        writeToLogWarn("ERROR NULL AIMLTRACE " + value + " -> " + childNode.OuterXml + "!");

                        if (tagHandlerChild != null)
                        {
                            IsOverBudget = tagHandlerChild.IsOverBudget;
                        }
                        value = Proc.processNodeDebug(childNode, query,
                                                 request, result, user,
                                                 this, copyChild, copyParent,
                                                 out tagHandlerChild);

                        success = false;
                    }
                    else
                    {
                        if (tagHandlerChild == null)
                        {
                            writeToLogWarn("ERROR tagHandlerChild AIMLTRACE " + value + " -> " + childNode.OuterXml + "!");
                            request.TimesOutAt = DateTime.Now + TimeSpan.FromMinutes(2); // for debugging
                            value = Proc.processNodeDebug(childNode, query,
                                                    request, result, user,
                                                    this, copyChild, copyParent,
                                                    out tagHandlerChild);
                        }
                        else
                        {
                            if (tagHandlerChild.QueryHasFailed) success = false;
                            if (tagHandlerChild.QueryHasSuceeded)
                                success = true;
                        }
                    }
                    if (saveOnInnerXML)
                    {
                        SaveResultOnChild(childNode, value);
                    }
                    return value;
                }
            }
            catch (ChatSignal ex)
            {
                throw;
            }
            catch (Exception e)
            {
                string value = "ERROR: " + e;
                writeToLog(value);
                success = false;
                return value;
            }
        }

        protected Unifiable Recurse()
        {
            bool _wasRecurseResultValid = innerResult.IsValid;
            try
            {
                if (innerResult.IsValid)
                {
                    writeToLog("USING CACHED RECURSE " + RecurseResult);
                    return CheckValue(innerResult.Value);
                    // use cached recurse value
                    return innerResult.Value;
                }
                Unifiable real = RecurseReal(templateNode, false);
                if (IsNullOrEmpty(real))
                {
                    if (IsStarAtomically)
                    {
                        if (IsNull(real)) return null;
                    }
                    if (IsMissing(real)) return null;
                    if (IsNull(real)) return null;
                    if (IsEMPTY(real)) return Unifiable.Empty;
                }
                if (real.AsString().Contains("<"))
                {
                    return real;
                }
                return real;
            }
            finally
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalse
                if (false && innerResult.IsValid != _wasRecurseResultValid)
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
                {
                    writeToLogWarn("_RecurseResultValid chged to " + innerResult.IsValid);
                }
                innerResult.IsValid = _wasRecurseResultValid;
            }
        }

        protected Unifiable RecurseReal(XmlNode node, bool saveOnChildren)
        {
            //Unifiable templateNodeInnerText;//= this.templateNodeInnerText;
            Unifiable templateResult = Unifiable.CreateAppendable();
            if (node.HasChildNodes)
            {
                ReadOnly = node.IsReadOnly;
                int goods = 0;
                // recursively check
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Comment) continue;
                    bool success;
                    Unifiable found = ProcessChildNode(childNode, ReadOnly, false, out success);
                    if (saveOnChildren)
                    {
                        if (success)
                            SaveResultOnChild(childNode, found);
                        else
                        {
                            writeToLogWarn("!success and writeToChild!");
                        }
                    }
                    if (IsNullOrEmpty(found) && !StaticAIMLUtils.IsSilentTag(childNode))
                    {
                        writeToLogWarn("IsNullOrEmpty: " + ToXmlValue(childNode));
                    }
                    templateResult.Append(found);
                    if (found == "" && StaticAIMLUtils.IsSilentTag(childNode))
                    {
                        goods++;
                        continue;
                    }
                    if (success)
                    {
                        QueryHasSuceeded = true;
                        continue;
                    }
                    if (found == "" && !IsSilentTag(childNode))
                    {
                        if (goods == 0) QueryHasFailed = true;
                    }
                    if (goods == 0) QueryHasFailed = true;
                }
                //templateNodeInnerText = templateResult;//.ToString();
                if (!templateResult.IsEmpty)
                {
                    templateResult = CheckValue(templateResult);
                    innerResult.Value = templateResult;
                    return templateResult;
                }
                if (QueryHasSuceeded) return templateResult;
                return templateResult;
            }
            else
            {
                if (IsStarAtomically)
                {
                    try
                    {
                        // atomic version of the node
                        Unifiable starContent = GetStarContent();
                        innerResult.Value = starContent;
                        if (!Unifiable.IsNullOrEmpty(starContent))
                            return starContent;
                    }
                    catch (ChatSignal ex)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        writeToLogWarn("ERROR {0}", e);
                    }
                }
                Unifiable before = Unifiable.InnerXmlText(node); //.InnerXml;        
                return CheckValue(before);
            }
        }

        /// <summary>
        /// Do a transformation on the Unifiable found in the InputString attribute
        /// </summary>
        /// <returns>The resulting transformed Unifiable</returns>
        public override string Transform()
        {
            if (!this.inputString.IsEmpty)
            {
                if (RecurseResultValid)
                {

                    return RecurseResult;
                }
                var finalResultValue = this.CompleteAimlProcess();// this.ProcessAimlChange();
                if (!Unifiable.IsNullOrEmpty(finalResultValue))
                {
                    RecurseResult = finalResultValue;
                    return finalResultValue;
                }
                if (!Unifiable.IsNull(finalResultValue))
                {
                    RecurseResult = finalResultValue;
                }
                return finalResultValue;
            }
            else
            {
                return Unifiable.Empty;
            }
        }

        public override Unifiable CompleteProcess()
        {
            if (RecurseResultValid) return RecurseResult;
            var vv = ProcessChange();
            RecurseResult = vv;
            DLRConsole.DepthCheck();
            if (!Unifiable.IsNullOrEmpty(vv))
            {
                return vv;
            }
            return vv;
        }
        /*
        public override Unifiable CompleteProcess()
        {
            if (RecurseResultValid)
            {
                return RecurseResult;
            }
            if (!IsStarted)
            {
                IsStarted = true;
                ProcessChange();
            }
            DLRConsole.DepthCheck();
            var vv = ProcessAimlChange();
            return vv;
        }*/
        /* public override Unifiable CompleteProcess()
         {
             //#if false
             return RecurseProcess();
         }*/
        private bool CheckReturnValue(Unifiable resultValue)
        {
            if (IsNull(resultValue))
            {
                ResetValues(true);
                var problem = "CompleteProcess() == NULL " + LineNumberTextInfo();
                writeToLogWarn(problem);
                Proc.TraceTest(problem, () => ProcessChange());
                return false;
            }
            if (resultValue.IsWildCard())
            {
                if (resultValue.IsLazyStar())
                {
                    Proc.TraceTest("XML IN RESULT: " + resultValue, () => ProcessChange());
                }
                return false;
            }
            return true;
        }

        protected Unifiable TestChildTagHandlers(IEnumerable<AIMLTagHandler> aimlTagHandlers)
        {
            Unifiable appendable = Unifiable.CreateAppendable();
            foreach (var childTagHandler in aimlTagHandlers)
            {
                var vv = childTagHandler.ProcessAimlChange();

                if (childTagHandler.QueryHasSuceeded)
                {
                    Unifiable checkValue = CheckValue(vv);
                    appendable.Append(checkValue);
                    this.QueryHasSuceededN++;
                }
                else if (childTagHandler.QueryHasFailed)
                {
                    this.QueryHasFailedN++;
                }
                else
                {
                    Unifiable checkValue = CheckValue(vv);
                    appendable.Append(checkValue);
                }

            }
            if (!IsEMPTY(appendable))
            {
                if (QueryHasSuceededN > 0 && QueryHasFailedN == 0)
                {
                    innerResult.Value = appendable;
                }
                return appendable;
            }
            return appendable;
        }

        protected IEnumerable<AIMLTagHandler> CreateChildTagHandlers(IEnumerable<XmlNode> xmlNodes)
        {
            List<AIMLTagHandler> aimlTagHandlers = new List<AIMLTagHandler>();
            foreach (var xmlNode in xmlNodes)
            {
                var childTagHandler = GetChildTagHandler(xmlNode);
                aimlTagHandlers.Add(childTagHandler);
            }
            return aimlTagHandlers;
        }


        public virtual Unifiable RecurseProcess()
        {
            //#endif
            if (RecurseResultValid)
            {
                return RecurseResult;
            }
            AIMLTagHandler tagHandler = this;
            Unifiable recursiveResult = null;
            if (tagHandler.isRecursive)
            {
                if (templateNode.HasChildNodes)
                {
                    recursiveResult = RecurseReal(templateNode, true);
                }
                else
                {
                    recursiveResult = InnerXmlText(templateNode);
                }
                string v = tagHandler.Transform();
                if (v == recursiveResult)
                {
                    return v;
                }
                /*
                if (Unifiable.IsNullOrEmpty(v))
                {
                    if (!Unifiable.IsNullOrEmpty(recursiveResult))
                        ;// v = recursiveResult;
                }
                 */
                return CheckValue(v);
            }
            else
            {
                string resultNodeInnerXML =  tagHandler.templateNode.OuterXml;//.ProcessChange();
                XmlNode resultNode = getNode("<template>" + resultNodeInnerXML + "</template>", templateNode);
                LineInfoElementImpl.unsetReadonly(resultNode);
                if (resultNode.HasChildNodes)
                {
                    recursiveResult = ProcessChildNode(resultNode);
                    if (!recursiveResult.IsEmpty)
                    {
                        RecurseResult = recursiveResult;
                        return recursiveResult;
                    }
                    recursiveResult = RecurseReal(resultNode, false);
                    if (!recursiveResult.IsEmpty)
                    {
                        RecurseResult = recursiveResult;
                        return recursiveResult;
                    }
                    return recursiveResult;
                }
                else
                {
                    return Unifiable.InnerXmlText(resultNode);
                }
            }
        }

        public virtual void SaveResultOnChild(XmlNode node, string value)
        {
            value = ValueText(value);
            if (InUnify)
            {
                return;
            }
            //if (value == null) return;
            //if (value == "") return;
            if (node.LocalName != "think")
                value = CheckValue(value);
            if (value == null || value.Trim() == "")
            {
                writeToLog("-!SaveResultOnChild AIMLTRACE " + value + " -> " + node.OuterXml);
            }
            if (node.NodeType == XmlNodeType.Comment) return;
            if (node is XmlText)
            {
                node.InnerText = XmlValueSettable(value);
            }
            else
            {
                node.InnerXml = XmlValueSettable(value);
            }
        }

        protected bool CheckNode(string name)
        {
            string templateNodeName = this.templateNode.Name;
            if (templateNodeName.ToLower() == name) return true;
            if (name.Contains(","))
            {
                string[] nameSplit = name.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in nameSplit)
                {
                    if (templateNodeName == s) return true;
                }
                foreach (string s in nameSplit)
                {
                    if (CheckNode(s)) return true;
                }
            }
            //writeToLog("CheckNode change " + name + " -> " + templateNode.Name);
            return true;
        }

        protected Unifiable GetActualValue(string name, string dictName, out bool succeed)
        {
            //ISettingsDictionary dict = query;
            return GetActualValue(templateNode, name, dictName, out succeed, query);
        }

        static protected Unifiable GetActualValue(XmlNode templateNode, string name, string dictNameIn, out bool succeed, SubQuery query)
        {
            ISettingsDictionary dict;// = query;
            Unifiable defaultVal = GetAttribValue(templateNode, "default,defaultValue", null);
            string dictName = GetNameOfDict(query, dictNameIn, templateNode, out dict);
            Unifiable gName = GetAttribValue(templateNode, "global_name", name);
            string realName;

            var prev = NamedValuesFromSettings.UseLuceneForSet;
            NamedValuesFromSettings.UseLuceneForSet = query.UseLuceneForSet;
            try
            {
                Unifiable v = NamedValuesFromSettings.GetSettingForType(
                    dictName, query, dict, name, out realName,
                    gName, defaultVal, out succeed, templateNode);
                return v;
            }
            finally
            {
                NamedValuesFromSettings.UseLuceneForSet = prev;
            }
        }

        protected virtual void AddSideEffect(string namedEffect, ThreadStart func)
        {
            if (InUnify)
            {
                writeToLogWarn("InUnify " + namedEffect);
                return;
            }
            if (QueryHasFailed)
            {
                writeToLog("SKIPPING " + namedEffect);
                return;
            }
            AIMLTagHandler tagHandler = FirstTagHandlerOrOuterMost("template");
            if (Parent == null || tagHandler == this || tagHandler == null)
            {
                query.AddSideEffect(namedEffect, func);
            }
            else
            {
                tagHandler.AddSideEffect(namedEffect, func);
            }
        }

        protected actMSM botActionMSM
        {
            get { return user.botActionMSM; }
        }
        /// <summary>
        /// Machine SideEffect - this denoates that he state of machine will change when processing the taghandler
        /// </summary>
        /// <param name="func"></param>
        protected virtual void MachineSideEffect(ThreadStart func)
        {
            LocalSideEffect(LineNumberTextInfo(), () =>
            {
                botActionMSM.PushSave();
                func();
            },
                            () => botActionMSM.PopLoad());
        }

        protected virtual void LocalSideEffect(string namedEffect, ThreadStart enter, ThreadStart exit)
        {
            if (QueryHasFailed)
            {
                writeToLog("SKIPPING LocalSideEffect " + namedEffect);
                return;
            }
            AIMLTagHandler tagHandler = FirstTagHandlerOrOuterMost("template");
            if (Parent == null || tagHandler == this || tagHandler == null)
            {
                query.LocalSideEffect(namedEffect, enter, exit);
            }
            else
            {
                tagHandler.LocalSideEffect(namedEffect, enter, exit);
            }
        }

        protected AIMLTagHandler FirstTagHandlerOrOuterMost(string type)
        {
            var current = this;
            while (true)
            {
                if (current.IsNode(type))
                {
                    return current;
                }
                if (null == current.Parent) return current;
                current = current.Parent;
            }
        }

        private bool IsNode(string type)
        {
            foreach (var c in StaticXMLUtils.NamesStrings(type))
            {
                if (SearchStringMatches(c, templateNode.Name))
                {
                    return true;
                }
            }
            return false;
        }


        public void AddChild(AIMLTagHandler part)
        {
            TagHandlerChilds = TagHandlerChilds ?? new List<AIMLTagHandler>();
            TagHandlerChilds.Add(part);
        }
        public void Dispose()
        {
            lock (this)
            {
                if (IsDisposing) return;
                IsDisposing = true;
            }
            if (TagHandlerChilds != null)
            {
                foreach (AIMLTagHandler child in TagHandlerChilds)
                {
                    child.Dispose();
                }
                TagHandlerChilds.Clear();
                TagHandlerChilds = null;
            }
            if (Parent != null) Parent.Dispose();
            return;
        }

        protected virtual ICollection<XmlNode> SelectNodes(XmlNodeList candidates)
        {
            List<XmlNode> matchedNodes = new List<XmlNode>();
            Unifiable outerName = GetAttribValue("name,var", null);
            Unifiable outerValue = GetAttribValue("value", null);

            bool outerNamePresent = !IsMissing(outerName);
            bool outerValuePresent = !IsMissing(outerValue);

            if (candidates == null || candidates.Count == 0)
            {
                return matchedNodes;
            }

            foreach (XmlNode childLINode in candidates)
            {
                string name = GetAttribValue(childLINode, "name,var", NullStringFunct, ReduceStarAttribute<string>);

                bool namePresent = !string.IsNullOrEmpty(name);
                if (!namePresent) name = outerName;


                Unifiable value = GetAttribValue<Unifiable>(childLINode, "value", () => null,
                    ReduceStarAttribute<Unifiable>);


                bool valuePresent = !IsMissing(value);
                if (!valuePresent) value = outerValue;

                //skip comments
                if (childLINode.LocalName.ToLower() == "#comment")
                {
                    //matchedNodes.Add(childLINode);
                    continue;
                }

                // non-<li> elements are freebies
                if (childLINode.LocalName.ToLower() != "li")
                {
                    matchedNodes.Add(childLINode);
                    continue;
                }

                if ((outerNamePresent || namePresent) && (valuePresent || outerValuePresent))
                {
                    // 1 name and 1 value (outer or inner + outer or inner)
                    bool succeed;
                    Unifiable actualValue = GetActualValue(childLINode, name, childLINode.Name, out succeed,
                                                           query);
                    if (IsPredMatch(value, actualValue, query))
                    {
                        matchedNodes.Add(childLINode);
                        continue;
                    }
                    continue;
                }
                // 0 names and 0 values default freebie
                if (!namePresent && !valuePresent)
                {
                    matchedNodes.Add(childLINode);
                    continue;
                }
                // name and value must be present
                if (outerNamePresent && !namePresent && !valuePresent && outerValuePresent)
                {
                    // 1 name and 1 value (outer + outer)
                    bool succeed;
                    Unifiable actualValue = GetActualValue(childLINode, name, childLINode.Name, out succeed,
                                   query);
                    if (IsPredMatch(value, actualValue, query))
                    {
                        matchedNodes.Add(childLINode);
                    }
                    continue;
                }
                if (!outerNamePresent && namePresent && !valuePresent && outerValuePresent)
                {
                    // 1 name and 1 value (inner + outer)
                    bool succeed;
                    Unifiable actualValue = GetActualValue(childLINode, name, childLINode.Name, out succeed,
                                   query);
                    if (IsPredMatch(value, actualValue, query))
                    {
                        matchedNodes.Add(childLINode);
                    }
                    continue;
                }
                if (!namePresent && !outerNamePresent)
                {
                    // 0 names and 0 values default freebie
                    if (!outerValuePresent && !valuePresent)
                    {
                        matchedNodes.Add(childLINode);
                        continue;
                    }
                    // 0 names and 2 values
                    if (outerValuePresent && valuePresent)
                    {
                        if (IsPredMatch(value, outerValue, query))
                        {
                            matchedNodes.Add(childLINode);
                        }
                        continue;
                    }
                }
                // 1 name and 1 value present
                if (!outerNamePresent && namePresent && valuePresent && !outerValuePresent)
                {
                    bool succeed;
                    Unifiable actualValue = GetActualValue(childLINode, name, childLINode.Name, out succeed,
                                                           query);
                    if (IsPredMatch(value, actualValue, query))
                    {
                        matchedNodes.Add(childLINode);
                    }
                    continue;
                }

                UnknownCondition();
            }
            if (matchedNodes.Count == 0)
            {
                writeToLogWarn("ERROR: no matched nodes " + LineNumberTextInfo());
            }
            return matchedNodes;
        }

        public void UnknownCondition()
        {
            writeToLogWarn("ERROR Unknown conditions " + LineNumberTextInfo());
        }
        protected Unifiable NodesToOutput(ICollection<XmlNode> nodes, Predicate<XmlNode> predicate)
        {
            List<AIMLTagHandler> aimlTagHandlers = new List<AIMLTagHandler>();
            foreach (var xmlNode in nodes)
            {
                if (predicate(xmlNode))
                {
                    var childTagHandler = GetChildTagHandler(xmlNode);
                    aimlTagHandlers.Add(childTagHandler);
                }
            }
            Unifiable appendable = TestChildTagHandlers(aimlTagHandlers);
            RecurseResult = appendable;
            return CheckValue(appendable);
        }
    }
}