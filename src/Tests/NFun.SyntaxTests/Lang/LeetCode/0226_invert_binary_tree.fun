# LeetCode 0226 — Invert Binary Tree
#
# Swap left and right of every node recursively.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun invert(t: tree?) -> tree?:
    if t == none: return none
    return tree {value = t.value, left = invert(t.right), right = invert(t.left)}

fun sameTree(p: tree?, q: tree?):
    if p == none and q == none: return true
    if p == none or q == none: return false
    if p.value != q.value: return false
    return sameTree(p.left, q.left) and sameTree(p.right, q.right)

@Test
fun testLeaf():
    t = tree {value = 1}
    assertEqual(sameTree(invert(t), t), true)

@Test
fun testTwoChildren():
    t = tree {value = 1, left = tree {value = 2}, right = tree {value = 3}}
    expected = tree {value = 1, left = tree {value = 3}, right = tree {value = 2}}
    assertEqual(sameTree(invert(t), expected), true)

@Test
fun testEmpty():
    assertEqual(invert(none) == none, true)
