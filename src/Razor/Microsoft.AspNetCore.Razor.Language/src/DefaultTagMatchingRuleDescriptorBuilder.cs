﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultTagMatchingRuleDescriptorBuilder : TagMatchingRuleDescriptorBuilder
    {
        private readonly DefaultTagHelperDescriptorBuilder _parent;
        private List<DefaultRequiredAttributeDescriptorBuilder> _requiredAttributeBuilders;
        private RazorDiagnosticCollection _diagnostics;

        internal DefaultTagMatchingRuleDescriptorBuilder(DefaultTagHelperDescriptorBuilder parent)
        {
            _parent = parent;
        }

        public override string TagName { get; set; }

        public override string ParentTag { get; set; }

        public override TagStructure TagStructure { get; set; }

        internal bool CaseSensitive => _parent.CaseSensitive;

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new RazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override IReadOnlyList<RequiredAttributeDescriptorBuilder> Attributes
        {
            get
            {
                EnsureRequiredAttributeBuilders();

                return _requiredAttributeBuilders;
            }
        }

        public override void Attribute(Action<RequiredAttributeDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureRequiredAttributeBuilders();

            var builder = new DefaultRequiredAttributeDescriptorBuilder(this);
            configure(builder);
            _requiredAttributeBuilders.Add(builder);
        }

        public TagMatchingRuleDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var requiredAttributes = Array.Empty<RequiredAttributeDescriptor>();
            if (_requiredAttributeBuilders != null)
            {
                var requiredAttributeSet = new HashSet<RequiredAttributeDescriptor>(RequiredAttributeDescriptorComparer.Default);
                for (var i = 0; i < _requiredAttributeBuilders.Count; i++)
                {
                    requiredAttributeSet.Add(_requiredAttributeBuilders[i].Build());
                }

                requiredAttributes = requiredAttributeSet.ToArray();
            }

            var rule = new DefaultTagMatchingRuleDescriptor(
                TagName,
                ParentTag,
                TagStructure,
                CaseSensitive,
                requiredAttributes,
                diagnostics.ToArray());

            return rule;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (string.IsNullOrWhiteSpace(TagName))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace();

                yield return diagnostic;
            }
            else if (TagName != TagHelperMatchingConventions.ElementCatchAllName)
            {
                foreach (var character in TagName)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagName(TagName, character);

                        yield return diagnostic;
                    }
                }
            }

            if (ParentTag != null)
            {
                if (string.IsNullOrWhiteSpace(ParentTag))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace();

                    yield return diagnostic;
                }
                else
                {
                    foreach (var character in ParentTag)
                    {
                        if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagName(ParentTag, character);

                            yield return diagnostic;
                        }
                    }
                }
            }
        }

        private void EnsureRequiredAttributeBuilders()
        {
            if (_requiredAttributeBuilders == null)
            {
                _requiredAttributeBuilders = new List<DefaultRequiredAttributeDescriptorBuilder>();
            }
        }
    }
}
