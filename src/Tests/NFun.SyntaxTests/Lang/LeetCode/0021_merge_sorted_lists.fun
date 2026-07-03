# LeetCode 0021 — Merge Two Sorted Lists
#
# Merge two sorted linked lists into one. Recursion picks the smaller head
# and recurses on the rest. Returns a fresh chain.

type node = {value: int, next: node? = none}

fun merge(a: node?, b: node?) -> node?:
    if a == none: return b
    if b == none: return a
    if a.value <= b.value:
        return node {value = a.value, next = merge(a.next, b)}
    return node {value = b.value, next = merge(a, b.next)}

fun toArray(n):
    out = []
    cur = n
    while cur != none:
        out = concat(out, [cur.value])
        cur = cur.next
    return out

@Test
fun testCanonical():
    a = node {value = 1, next = node {value = 2, next = node {value = 4}}}
    b = node {value = 1, next = node {value = 3, next = node {value = 4}}}
    assertEqual(toArray(merge(a, b)), [1, 1, 2, 3, 4, 4])

@Test
fun testOneEmpty():
    a = node {value = 5}
    assertEqual(toArray(merge(a, none)), [5])
    assertEqual(toArray(merge(none, a)), [5])

@Test
fun testBothEmpty():
    assertEqual(merge(none, none) == none, true)
