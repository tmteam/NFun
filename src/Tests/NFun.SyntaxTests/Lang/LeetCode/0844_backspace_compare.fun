# LeetCode 0844 — Backspace String Compare
#
# `#` deletes the previous character. Build the typed strings without
# explicit stacks: a char array we shrink via take()/concat.

fun apply(s):
    out = ''
    for c in s:
        if c == /'#':
            if out.count() > 0: out = out.take(out.count() - 1)
        else:
            out = concat(out, '{c}')
    return out

fun backspaceCompare(s, t):
    return apply(s) == apply(t)

@Test('ab#c', 'ad#c', true)
@Test('ab##', 'c#d#', true)
@Test('a#c', 'b', false)
@Test('a##c', '#a#c', true)
fun testBackspaceCompare(s, t, expected):
    assertEqual(backspaceCompare(s, t), expected)
