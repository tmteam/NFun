# LeetCode 0100 — Same Tree
#
# Given the roots of two binary trees, return true iff they're structurally
# identical and node values match.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun isSameTree(p, q):
    if p == none and q == none: return true
    if p == none or q == none: return false
    if p.value != q.value: return false
    return isSameTree(p.left, q.left) and isSameTree(p.right, q.right)

@Test
fun testIdentical():
    a = tree {
        value = 1
        left = tree {value = 2}
        right = tree {value = 3}
    }
    b = tree {
        value = 1
        left = tree {value = 2}
        right = tree {value = 3}
    }
    assertEqual(isSameTree(a, b), true)

@Test
fun testDifferentValue():
    a = tree {
        value = 1
        left = tree {value = 2}
    }
    b = tree {
        value = 1
        left = tree {value = 99}
    }
    assertEqual(isSameTree(a, b), false)

@Test
fun testDifferentShape():
    a = tree {
        value = 1
        left = tree {value = 2}
    }
    b = tree {
        value = 1
        right = tree {value = 2}
    }
    assertEqual(isSameTree(a, b), false)

@Test
fun testBothEmpty():
    assertEqual(isSameTree(none, none), true)

@Test
fun testOneEmpty():
    leaf = tree {value = 1}
    assertEqual(isSameTree(leaf, none), false)
    assertEqual(isSameTree(none, leaf), false)
