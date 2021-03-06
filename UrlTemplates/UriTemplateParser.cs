﻿namespace Resta.UriTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class UriTemplateParser
    {
        private string template;
        private List<IUriComponent> components;
        private StringBuilder builder;
        private Token token;
        private int position;
        private Operator exprOperator;
        private List<VarSpec> varSpecs;
        private bool varSpecExloded;
        private int varSpecMaxLength;

        public UriTemplateParser(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }

            this.template = template;
        }

        private enum Token
        {
            Literal,
            Expression,
            VarSpec,
            VarSpecMaxLength,
            VarSpecExploded
        }

        public static List<IUriComponent> Parse(string template)
        {
            return new UriTemplateParser(template).Parse();
        }

        private List<IUriComponent> Parse()
        {
            try
            {
                builder = new StringBuilder();
                components = new List<IUriComponent>();
                token = Token.Literal;

                for (position = 0; position < template.Length; position++)
                {
                    var ch = template[position];

                    switch (token)
                    {
                        case Token.Literal:
                            ReadLiteral(ch);
                            break;

                        case Token.Expression:
                            ReadExpression(ch);
                            break;

                        case Token.VarSpec:
                            ReadVarSpec(ch);
                            break;

                        case Token.VarSpecExploded:
                            ReadVarSpecExploded(ch);
                            break;

                        case Token.VarSpecMaxLength:
                            ReadVarSpecMaxLength(ch);
                            break;
                    }
                }

                if (token != Token.Literal)
                {
                    ThrowException("Unexpected end of URI template");
                }

                CreateLiteral();
            }
            catch (UriTemplateException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new UriTemplateParseException(string.Format("Error at parse URI template: {0}", exception.Message), template, position, exception);
            }

            return components;
        }

        private void ReadLiteral(char ch)
        {
            if (ch == '{')
            {
                token = Token.Expression;
            }
            else if (ch == '}')
            {
                ThrowException("Invalid literal character \"}\"");
            }
            else
            {
                builder.Append(ch);
            }
        }

        private void ReadExpression(char ch)
        {
            CreateLiteral();

            token = Token.VarSpec;

            if (!Operator.TryParse(ch, out exprOperator))
            {
                exprOperator = Operator.Default;
                ReadVarSpec(ch);
            }
        }

        private void ReadVarSpec(char ch)
        {
            if (TryReadEndVarSpec(ch))
            {
                return;
            }

            if (ch == '*')
            {
                token = Token.VarSpecExploded;
            }
            else if (ch == ':')
            {
                varSpecMaxLength = -1;
                token = Token.VarSpecMaxLength;
            }
            else if (!CharSpec.IsVarChar(ch))
            {
                ThrowException("Invalid name of template variable.");
            }
            else
            {
                builder.Append(ch);
            }
        }

        private void ReadVarSpecMaxLength(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                var digit = ch - '0';

                if (varSpecMaxLength == -1)
                {
                    varSpecMaxLength = digit;
                }
                else
                {
                    varSpecMaxLength = varSpecMaxLength * 10 + digit;
                }
            }
            else if (!TryReadEndVarSpec(ch))
            {
                ThrowException("Invalid URI template length modifier");
            }
        }

        private void ReadVarSpecExploded(char ch)
        {
            varSpecExloded = true;

            if (!TryReadEndVarSpec(ch))
            {
                ThrowException("Invalid URI template modifier");
            }
        }

        private bool TryReadEndVarSpec(char ch)
        {
            if (ch == '}' || ch == ',')
            {
                CreateVarSpec();

                if (ch == '}')
                {
                    CreateExpression();
                    token = Token.Literal;
                }
                else
                {
                    token = Token.VarSpec;
                }

                return true;
            }

            return false;
        }

        private void CreateExpression()
        {
            var expr = new Expression(exprOperator, varSpecs);
            components.Add(expr);
            varSpecs = null;
        }

        private void CreateVarSpec()
        {
            if (varSpecMaxLength == -1)
            {
                ThrowException("Invalid URI template modifier.");
            }

            var name = builder.ToString();
            var varSpec = new VarSpec(name, varSpecExloded, varSpecMaxLength, true);

            if (varSpecs == null)
            {
                varSpecs = new List<VarSpec>();
            }

            varSpecs.Add(varSpec);

            builder.Length = 0;
            varSpecExloded = false;
            varSpecMaxLength = 0;
        }

        private void CreateLiteral()
        {
            if (builder.Length != 0)
            {
                components.Add(new Literal(builder.ToString()));
                builder.Length = 0;
            }
        }

        private void ThrowException(string message)
        {
            throw new UriTemplateParseException(message, template, position);
        }
    }
}