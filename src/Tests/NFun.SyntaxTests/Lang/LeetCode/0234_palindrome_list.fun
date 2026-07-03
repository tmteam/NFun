# LeetCode 0234 — Palindrome Linked List
#
# A linked list is a palindrome iff its values, read forward, equal their
# reverse. Convert to an array first, then two-pointer check.

type node = {value: int, next: node? = none}

fun toArray(n):
    out = []
    cur = n
    while cur != none:
        out = concat(out, [cur.value])
        cur = cur.next
    return out

fun isPalindromeList(head):
    arr = toArray(head)
    lo = 0
    hi = arr.count() - 1
    while lo < hi:
        if arr[lo] != arr[hi]: return false
        lo += 1
        hi -= 1
    return true

@Test
fun testPalindrome():
    head = node {
        value = 1
        next = node {
            value = 2
            next = node {
                value = 2
                next = node {value = 1}
            }
        }
    }
    assertEqual(isPalindromeList(head), true)

@Test
fun testNotPalindrome():
    head = node {
        value = 1
        next = node {value = 2}
    }
    assertEqual(isPalindromeList(head), false)

@Test
fun testEmpty():
    assertEqual(isPalindromeList(none), true)

@Test
fun testSingle():
    assertEqual(isPalindromeList(node {value = 42}), true)
