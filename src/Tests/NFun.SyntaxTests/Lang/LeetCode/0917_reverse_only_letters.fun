# LeetCode 0917 — Reverse Only Letters
#
# Reverse the letters in s while keeping every non-letter at its original
# index. Two-pointer swap-via-rebuild.

fun isLetter(c):
    return (c >= /'a' and c <= /'z') or (c >= /'A' and c <= /'Z')

fun reverseOnlyLetters(s):
    n = s.count()
    # Collect letters in reverse order; consume that pool when rebuilding.
    letters = []
    i = n - 1
    while i >= 0:
        if isLetter(s[i]): letters = concat(letters, [s[i]])
        i -= 1
    out = ''
    li = 0
    i = 0
    while i < n:
        if isLetter(s[i]):
            out = concat(out, '{letters[li]}')
            li += 1
        else:
            out = concat(out, '{s[i]}')
        i += 1
    return out

@Test('ab-cd', 'dc-ba')
@Test('a-bC-dEf-ghIj', 'j-Ih-gfE-dCba')
@Test('Test1ng-Leet=code-Q!', 'Qedo1ct-eeLg=ntse-T!')
@Test('', '')
fun testReverseOnly(s, expected):
    assertEqual(reverseOnlyLetters(s), expected)
