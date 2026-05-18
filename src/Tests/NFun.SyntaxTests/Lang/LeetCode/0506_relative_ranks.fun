# LeetCode 0506 — Relative Ranks
#
# Given scores, return medal/rank string per index.

fun rankLabel(r):
    if r == 0: return 'Gold Medal'
    if r == 1: return 'Silver Medal'
    if r == 2: return 'Bronze Medal'
    return '{r + 1}'

fun findRelativeRanks(score:int[]):
    sortedDesc = score.sort().reverse()
    return score.map(rule rankLabel(sortedDesc.find(it)))

@Test
fun testCanonical():
    assertEqual(findRelativeRanks([5, 4, 3, 2, 1]),
                ['Gold Medal', 'Silver Medal', 'Bronze Medal', '4', '5'])

@Test
fun testSingle():
    assertEqual(findRelativeRanks([1]), ['Gold Medal'])

@Test
fun testTwo():
    assertEqual(findRelativeRanks([10, 5]), ['Gold Medal', 'Silver Medal'])

@Test
fun testReverse():
    assertEqual(findRelativeRanks([1, 2, 3, 4, 5]),
                ['5', '4', 'Bronze Medal', 'Silver Medal', 'Gold Medal'])
