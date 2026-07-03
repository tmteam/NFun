# LeetCode 0083 — Remove Duplicates from Sorted List
#
# Sorted linked list — keep one copy of each distinct value. Build a fresh
# list via recursion.

type node = {value: int, next: node? = none}

fun dedupe(n: node?) -> node?:
    if n == none: return none
    rest = dedupe(n.next)
    if rest != none and rest.value == n.value:
        return rest
    return node {value = n.value, next = rest}

fun toArray(n):
    out = []
    cur = n
    while cur != none:
        out = concat(out, [cur.value])
        cur = cur.next
    return out

@Test
fun testCanonical():
    head = node {value = 1, next = node {value = 1, next = node {value = 2}}}
    assertEqual(toArray(dedupe(head)), [1, 2])

@Test
fun testAllSame():
    head = node {value = 7, next = node {value = 7, next = node {value = 7}}}
    assertEqual(toArray(dedupe(head)), [7])

@Test
fun testEmpty():
    assertEqual(dedupe(none) == none, true)

@Test
fun testNoDuplicates():
    head = node {value = 1, next = node {value = 2, next = node {value = 3}}}
    assertEqual(toArray(dedupe(head)), [1, 2, 3])
