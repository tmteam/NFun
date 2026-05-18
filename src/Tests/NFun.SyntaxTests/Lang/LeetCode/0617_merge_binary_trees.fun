# LeetCode 0617 — Merge Two Binary Trees
#
# Build a new tree summing overlapping nodes; missing positions inherit the
# other tree's subtree.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun mergeTrees(a: tree?, b: tree?) -> tree?:
    if a == none and b == none: return none
    if a == none: return b
    if b == none: return a
    return tree {
        value = a.value + b.value,
        left  = mergeTrees(a.left,  b.left),
        right = mergeTrees(a.right, b.right),
    }

fun sameTree(p: tree?, q: tree?):
    if p == none and q == none: return true
    if p == none or q == none: return false
    if p.value != q.value: return false
    return sameTree(p.left, q.left) and sameTree(p.right, q.right)

@Test
fun testBothEmpty():
    assertEqual(mergeTrees(none, none) == none, true)

@Test
fun testOneEmpty():
    t = tree {value = 7}
    assertEqual(sameTree(mergeTrees(t, none), t), true)
    assertEqual(sameTree(mergeTrees(none, t), t), true)

@Test
fun testOverlap():
    a = tree {value = 1, left = tree {value = 2}}
    b = tree {value = 3, right = tree {value = 4}}
    expected = tree {value = 4, left = tree {value = 2}, right = tree {value = 4}}
    assertEqual(sameTree(mergeTrees(a, b), expected), true)
