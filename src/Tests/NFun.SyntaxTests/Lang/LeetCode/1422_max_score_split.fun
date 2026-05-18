# LeetCode 1422 — Maximum Score After Splitting a String
#
# Split into non-empty left/right. Score = zeros in left + ones in right.
# One pass: pre-count ones in total, sweep left building zeros/ones.

fun maxScore(s):
    totalOnes = 0
    for c in s:
        if c == /'1': totalOnes += 1
    leftZeros = 0
    leftOnes = 0
    best = 0
    i = 0
    while i < s.count() - 1:
        if s[i] == /'0':
            leftZeros += 1
        else:
            leftOnes += 1
        rightOnes = totalOnes - leftOnes
        score = leftZeros + rightOnes
        if score > best: best = score
        i += 1
    return best

@Test('011101', 5)
@Test('00111', 5)
@Test('1111', 3)
fun testMaxScore(s, expected):
    assertEqual(maxScore(s), expected)
