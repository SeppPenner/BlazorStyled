﻿using BlazorStyled.Stylesheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorStyled.Internal
{
    internal class Styled : IStyled
    {
        private const string DEFAULT = "Default";
        private readonly IStyleSheet _styleSheet;
        private readonly string _id;

        public Styled(IStyleSheet styleSheet)
        {
            _styleSheet = styleSheet;
            _id = DEFAULT;
        }

        private Styled(IStyleSheet styleSheet, string id)
        {
            _styleSheet = styleSheet;
            _id = id;
        }

        public Theme Theme { get; set; } = new Theme();

        public string Css(string className, string css)
        {
            try
            {
                css = css.RemoveDuplicateSpaces();
                IRule rule;
                if (className.IndexOf("@font-face") != -1)
                {
                    rule = ParseFontFace(css);
                    _styleSheet.AddClass(rule, _id);
                }
                else if (className.IndexOf("@media") != -1)
                {
                    rule = ParseMediaQuery(className, "{" + css + "}");
                    _styleSheet.AddClass(rule, _id);
                }
                else
                {
                    rule = ParsePredefinedRuleSet(className, css);
                    if (_elements.Contains(className))
                    {
                        _styleSheet.AddClass(rule, _id);
                    }
                    else
                    {
                        _styleSheet.AddClass(rule, _id);
                    }
                }
                return rule.Selector;
            }
            catch (StyledException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw StyledException.GetException(css, e);
            }
        }

        public string Css(string css)
        {
            try
            {
                css = css.RemoveDuplicateSpaces();
                RuleSet ruleSet = ParseRuleSet(css);
                if (ruleSet.Declarations.Count > 0)
                {
                    _styleSheet.AddClass(ruleSet, _id);
                }
                foreach (IRule nestedRuleSet in ruleSet.NestedRules)
                {
                    _styleSheet.AddClass(nestedRuleSet, _id);
                }
                return ruleSet.Selector;
            }
            catch (StyledException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw StyledException.GetException(css, e);
            }
        }

        public string Css(List<string> classes, string css)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string cssClass in classes)
            {
                string result = Css(cssClass, css);
                sb.Append(result).Append(' ');
            }
            return sb.ToString().Trim();
        }

        public string Keyframes(string css)
        {
            try
            {
                css = css.RemoveDuplicateSpaces();
                Keyframe keyframe = ParseKeyframe(css);
                _styleSheet.AddClass(keyframe, _id);
                return keyframe.Selector;
            }
            catch (StyledException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw StyledException.GetException(css, e);
            }
        }

        public void Fontface(string css)
        {
            try
            {
                css = css.RemoveDuplicateSpaces();
                FontFace fontface = ParseFontFace(css);
                _styleSheet.AddClass(fontface, _id);
            }
            catch (StyledException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw StyledException.GetException(css, e);
            }
        }

        public void ClearStyles()
        {
            _styleSheet.ClearStyles(_id);
        }

        public void AddGoogleFonts(List<GoogleFont> googleFonts)
        {
            string fontString = string.Join("|", googleFonts.Select(googleFont => googleFont.Name.Replace(' ', '+') + ':' + string.Join(",", googleFont.Styles)));
            string uri = $"//fonts.googleapis.com/css?family={fontString}&display=swap";
            _styleSheet.AddClass(new ImportUri(uri), _id);
        }

        public IStyled WithId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = DEFAULT;
            }
            return new Styled(_styleSheet, id.Replace(" ", "-"));
        }

        private IRule ParseMediaQuery(string classname, string css)
        {
            IRule mediaQuery = new MediaQuery
            {
                Selector = classname
            };
            int first = css.IndexOf('{') + 1;
            int last = css.LastIndexOf('}');
            string parsed = css.Substring(first, last - first).Trim();
            mediaQuery.NestedRules = ParseRuleSet(parsed).NestedRules;
            return mediaQuery;
        }

        private Keyframe ParseKeyframe(string css)
        {
            Keyframe keyframe = new Keyframe();
            IRule current = keyframe;
            string buffer = string.Empty;
            bool nestedClassClosed = true;
            foreach (char ch in css)
            {
                switch (ch)
                {
                    case ';':
                        Declaration declaration = ParseDeclaration(buffer.Trim());
                        if (declaration != null)
                        {
                            current.Declarations.Add(declaration);
                        }
                        buffer = string.Empty;
                        break;
                    case '{':
                        IRule nestedClass;
                        nestedClass = new PredefinedRuleSet
                        {
                            Selector = buffer.Trim()
                        };
                        keyframe.NestedRules.Add(nestedClass);
                        buffer = string.Empty;
                        current = nestedClass;
                        nestedClassClosed = false;
                        break;
                    case '}':
                        nestedClassClosed = true;
                        break;
                    default:
                        buffer += ch;
                        break;
                }
            }
            if (!nestedClassClosed)
            {
                throw StyledException.GetException(css, "A nested class is missing a '}' character", null);
            }
            if (buffer.Trim() != string.Empty)
            {
                throw StyledException.GetException(buffer, "This is usually caused by a missing ';' character at the end of a declaration", null);
            }
            keyframe.SetClassName();
            return keyframe;
        }

        private FontFace ParseFontFace(string css)
        {
            FontFace fontface = new FontFace
            {
                Declarations = ParseDeclarations(css)
            };
            fontface.SetClassName();
            return fontface;
        }

        private PredefinedRuleSet ParsePredefinedRuleSet(string className, string css)
        {
            PredefinedRuleSet predefinedRuleSet = new PredefinedRuleSet { Selector = className.Trim() };
            predefinedRuleSet.Declarations = ParseDeclarations(css);
            return predefinedRuleSet;
        }

        private List<Declaration> ParseDeclarations(string css)
        {
            List<Declaration> declarations = new List<Declaration>();
            string[] declarationsString = css.Trim().Split(';');
            foreach (string declarationString in declarationsString)
            {
                if (declarationString.IndexOf(':') != -1)
                {
                    Declaration declaration = ParseDeclaration(declarationString.Trim());
                    if (declaration != null)
                    {
                        declarations.Add(declaration);
                    }
                }
            }
            return declarations;
        }

        private RuleSet ParseRuleSet(string css)
        {
            RuleSet ruleSet = new RuleSet
            {
                Label = _id != DEFAULT ? _id : null
            };
            IRule current = ruleSet;
            string buffer = string.Empty;
            bool nestedClassClosed = true; //Start from true becuase the parent doesnt need to be closed
            foreach (char ch in css)
            {
                switch (ch)
                {
                    case ';':
                        Declaration declaration = ParseDeclaration(buffer.Trim());
                        if (declaration != null)
                        {
                            if (declaration.Property == "label")
                            {
                                current.Label = current.Label == null ? declaration.Value.Trim() : $"{current.Label}-{declaration.Value.Trim()}";
                            }
                            else
                            {
                                current.Declarations.Add(declaration);
                            }
                        }
                        buffer = string.Empty;
                        break;
                    case '{':
                        IRule nestedClass;
                        if (buffer.IndexOf("@media") == -1)
                        {
                            nestedClass = new PredefinedRuleSet
                            {
                                Selector = buffer.Trim()
                            };
                        }
                        else
                        {
                            nestedClass = new MediaQuery
                            {
                                Selector = buffer.Trim() + "{&"
                            };
                        }
                        ruleSet.NestedRules.Add(nestedClass);
                        buffer = string.Empty;
                        current = nestedClass;
                        nestedClassClosed = false;
                        break;
                    case '}':
                        nestedClassClosed = true;
                        break;
                    default:
                        buffer += ch;
                        break;
                }
            }
            if (!nestedClassClosed)
            {
                throw StyledException.GetException(css, "A nested class is missing a '}' character", null);
            }
            if (buffer.Trim() != string.Empty)
            {
                throw StyledException.GetException(buffer, "This is usually caused by a missing ';' character at the end of a declaration", null);
            }
            ruleSet.SetClassName();
            return ruleSet;
        }

        private Declaration ParseDeclaration(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            try
            {
                string property = input.Substring(0, input.IndexOf(':')).Trim();
                string value = input.Substring(input.IndexOf(':') + 1).Trim();
                return new Declaration { Property = property, Value = value };
            }
            catch (Exception e)
            {
                throw StyledException.GetException(input, "This is likely cause by a missing ':' character", e);
            }
        }

        private readonly List<string> _elements = new List<string>
        {
            "a",
            "abbr",
            "address",
            "area",
            "article",
            "aside",
            "audio",
            "b",
            "base",
            "bdi",
            "bdo",
            "blockquote",
            "body",
            "br",
            "button",
            "canvas",
            "caption",
            "cite",
            "code",
            "col",
            "colgroup",
            "data",
            "datalist",
            "dd",
            "del",
            "details",
            "dfn",
            "dialog",
            "div",
            "dl",
            "dt",
            "em",
            "embed",
            "fieldset",
            "figcaption",
            "figure",
            "footer",
            "form",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "head",
            "header",
            "hr",
            "html",
            "i",
            "iframe",
            "img",
            "input",
            "ins",
            "kbd",
            "label",
            "legend",
            "li",
            "link",
            "main",
            "map",
            "mark",
            "meta",
            "meter",
            "nav",
            "noscript",
            "object",
            "ol",
            "optgroup",
            "option",
            "output",
            "p",
            "param",
            "picture",
            "pre",
            "progress",
            "q",
            "rp",
            "rt",
            "ruby",
            "s",
            "samp",
            "script",
            "section",
            "select",
            "small",
            "source",
            "span",
            "strong",
            "style",
            "sub",
            "summary",
            "sup",
            "svg",
            "table",
            "tbody",
            "td",
            "template",
            "textarea",
            "tfoot",
            "th",
            "thead",
            "time",
            "title",
            "tr",
            "track",
            "u",
            "ul",
            "var",
            "video",
            "wbr"
        };
    }
}
