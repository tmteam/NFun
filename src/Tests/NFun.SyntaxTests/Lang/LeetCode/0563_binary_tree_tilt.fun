# LeetCode 0563 — Binary Tree Tilt
#
# Tilt of a node = abs(sum(left subtree) - sum(right subtree)).
# Tree tilt = sum of all node tilts.
#
# Post-order returns (subtree sum, accumulated tilt). NFun has no tuples so
# a `result` struct does the job.

type tree = {value: int, left: tree? = none, right: tree? = none}
type result = {sum: int, tilt: int}

fun walk(t):
    if t == none: return result {sum = 0, tilt = 0}
    L = walk(t!.left)
    R = walk(t!.right)
    return result {
        sum = t!.value + L.sum + R.sum
        tilt = L.tilt + R.tilt + abs(L.sum - R.sum)
    }

fun findTilt(t):
    return walk(t).tilt

@Test
fun testEmpty():
    assertEqual(findTilt(none), 0)

@Test
fun testSingle():
    assertEqual(findTilt(tree {value = 1}), 0)

@Test
fun testCanonical():
    #     1                tilts:
    #    / \                root: |2 - 3| = 1
    #   2   3               2:    0
    #                       3:    0       total = 1
    root = tree {
        value = 1
        left = tree {value = 2}
        right = tree {value = 3}
    }
    assertEqual(findTilt(root), 1)
