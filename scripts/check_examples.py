#!/usr/bin/env python3
"""
Script to analyze all examples for potential parsing issues.
"""

import json

def split_preserving_quotes(command):
    """Simulate the C# parsing logic"""
    parts = []
    current = []
    in_quotes = False
    quote_char = '"'
    brace_depth = 0
    paren_depth = 0

    for c in command:
        if c in ('"', "'") and not in_quotes and brace_depth == 0 and paren_depth == 0:
            in_quotes = True
            quote_char = c
            current.append(c)
        elif c == quote_char and in_quotes:
            in_quotes = False
            current.append(c)
        elif c == '{' and not in_quotes:
            brace_depth += 1
            current.append(c)
        elif c == '}' and not in_quotes:
            brace_depth = max(0, brace_depth - 1)
            current.append(c)
        elif c == '(' and not in_quotes:
            paren_depth += 1
            current.append(c)
        elif c == ')' and not in_quotes:
            paren_depth = max(0, paren_depth - 1)
            current.append(c)
        elif c == ' ' and not in_quotes and brace_depth == 0 and paren_depth == 0:
            if current:
                parts.append(''.join(current))
                current = []
        else:
            current.append(c)

    if current:
        parts.append(''.join(current))

    return parts, in_quotes, brace_depth, paren_depth

def check_examples(input_file):
    with open(input_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    problems = []

    for action in data['actions']:
        for ex in action.get('examples', []):
            cmd = ex.get('command', '')
            if not cmd or '<' in cmd:  # Skip templates
                continue

            # Check for pipe - only parse before pipe
            pipe_idx = cmd.find('|')
            if pipe_idx > 0:
                cmd_to_parse = cmd[:pipe_idx].strip()
            else:
                cmd_to_parse = cmd

            parts, in_quotes, brace_depth, paren_depth = split_preserving_quotes(cmd_to_parse)

            # Detect problems
            issue = None
            if in_quotes:
                issue = 'Unclosed quotes'
            elif brace_depth > 0:
                issue = f'Unclosed braces ({brace_depth})'
            elif paren_depth > 0:
                issue = f'Unclosed parens ({paren_depth})'

            # Check for potentially problematic patterns in parts
            for part in parts:
                # Check for unbalanced quotes/braces in individual parts
                if part.count('"') % 2 != 0:
                    issue = f'Unbalanced quotes in: {part[:40]}'
                if part.count('{') != part.count('}'):
                    issue = f'Unbalanced braces in: {part[:40]}'
                if part.count('(') != part.count(')'):
                    issue = f'Unbalanced parens in: {part[:40]}'

            if issue:
                problems.append({
                    'action': action['title'],
                    'action_id': action['id'],
                    'command': cmd[:100],
                    'issue': issue
                })

    print(f'Found {len(problems)} potential issues:\n')
    for p in problems:
        print(f"Action: {p['action']}")
        print(f"  Command: {p['command']}...")
        print(f"  Issue: {p['issue']}\n")

    return problems

if __name__ == '__main__':
    input_file = r'G:\_dev\TwinShell\TwinShell\data\seed\initial-actions.json'
    check_examples(input_file)
