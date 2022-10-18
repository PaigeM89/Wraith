namespace Wraith.Tests

open Expecto


module PagingTests =
  open Wraith
  open Wraith.ListPrompts

  let smoothScrollTests = testList "Smooth scrolling paging tests" [
    testCase "Calculates page count when there is only 1 page" <| fun _ ->
      let input = SmoothScroll(10, 0, 10)
      let output = input.MaxPages 10
      Expect.equal output 1 "Should only calculate a single page"
    testCase "Calculates page count when there is 2 pages" <| fun _ ->
      let input = SmoothScroll(10, 0, 10)
      let output = input.MaxPages 11
      Expect.equal output 2 "Should calculate 2 pages"
    testCase "Does not go below 0 on minimum index" <| fun _ ->
      let input = SmoothScroll(10, 0, 10)
      let output = input.Previous()
      match output with
      | SmoothScroll(_, min, _) ->
        Expect.equal min 0 "Should not set minimum below 0"
      | _ ->
        failwith "Should be a paging type of Smooth Scroll"
    testCase "Says there is a page before when min is not 0" <| fun _ ->
      let input = SmoothScroll(10, 1, 10)
      let output = input.HasPageBefore()
      Expect.isTrue output "Should find a page above the current scroll page"
    testCase "Says there is a page after when options size is less than current max" <| fun _ ->
      let input = SmoothScroll(10, 1, 10)
      let output = input.HasPageAfter 11
      Expect.isTrue output "Should find a page after the current scroll page"
    testCase "Says there is not a page after when the last option is visible on the current page" <| fun _ ->
      let input = SmoothScroll(10, 1, 10)
      let output = input.HasPageAfter 10
      Expect.isFalse output "Should not find a page after the current scroll page"
  ]

  let jumpScrollTests = testList "Jump scrolling paging tests" [
    testCase "Calculates page count when there is only 1 page" <| fun _ ->
      let input: PagingType = JumpScroll(10, 0)
      let output = input.MaxPages 10
      Expect.equal output 1 "Should only calculate a single page"
    testCase "Calculates page count when there is 2 pages" <| fun _ ->
      let input = JumpScroll(10, 0)
      let output = input.MaxPages 11
      Expect.equal output 2 "Should calculate 2 pages"
    testCase "Does not go below 0 on minimum page" <| fun _ ->
      let input = JumpScroll(10, 0)
      let output = input.Previous()
      match output with
      | JumpScroll(_, pageNumber) ->
        Expect.equal pageNumber 0 "Should not set page below 0"
      | _ ->
        failwith "Should be a paging tyupe of jump scroll"
    testCase "Says there is a page before when current page is not 0" <| fun _ ->
      let input = JumpScroll(10, 1)
      let output = input.HasPageBefore()
      Expect.isTrue output "Should indiciate a page before the current page"
    testCase "Says there is a page after when the current page is less than the max possible page" <| fun _ ->
      let input = JumpScroll(10, 1)
      let output = input.HasPageAfter 21
      Expect.isTrue output "Should find a page after the current page"
    testCase "Says there is not a page after when the current page renders the last item in the list" <| fun _ ->
      let input = JumpScroll(10, 1)
      let output = input.HasPageAfter 20
      Expect.isFalse output $"Should not find a page after the current page. Max pages: %i{input.MaxPages 20}"
  ]

  let tests = testList "Paging tests" [
    smoothScrollTests
    jumpScrollTests
  ]