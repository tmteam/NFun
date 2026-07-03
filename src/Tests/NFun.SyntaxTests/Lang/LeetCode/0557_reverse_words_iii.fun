# LeetCode 0557 — Reverse Words in a String III
#
# Reverse each space-delimited word while keeping word order. Scan, buffer
# the current word's chars, flush reversed on space or end.

fun reverseWords(s):
    out = ''
    buf = ''
    i = 0
    while i < s.count():
        c = s[i]
        if c == /' ':
            out = concat(concat(out, buf.reverse()), ' ')
            buf = ''
        else:
            buf = concat(buf, '{c}')
        i += 1
    return concat(out, buf.reverse())

@Test('Let\'s take LeetCode contest', 's\'teL ekat edoCteeL tsetnoc')
@Test('Mr Ding', 'rM gniD')
@Test('a', 'a')
@Test('', '')
fun testReverseWords(s, expected):
    assertEqual(reverseWords(s), expected)
