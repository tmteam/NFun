# LeetCode 0872 — Leaf-Similar Trees
#
# Two binary trees are leaf-similar if the left-to-right leaf sequences match.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun leaves(t: tree?):
    if t == none: return []
    if t.left == none and t.right == none: return [t.value]
    return concat(leaves(t.left), leaves(t.right))

fun leafSimilar(a: tree?, b: tree?):
    return leaves(a) == leaves(b)

@Test
fun testIdenticalLeaves():
    a = tree {value = 1, left = tree {value = 2}, right = tree {value = 3}}
    b = tree {value = 9, left = tree {value = 2}, right = tree {value = 3}}
    assertEqual(leafSimilar(a, b), true)

@Test
fun testDifferentLeaves():
    a = tree {value = 1, left = tree {value = 2}}
    b = tree {value = 1, left = tree {value = 5}}
    assertEqual(leafSimilar(a, b), false)

@Test
fun testBothEmpty():
    assertEqual(leafSimilar(none, none), true)
