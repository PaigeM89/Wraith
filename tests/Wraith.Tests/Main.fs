module WraithTests

open Wraith.Tests
open Expecto

let tests =
  testList "Wraith tests" [
    PagingTests.tests
  ]

[<EntryPoint>]
let main args =
  runTestsWithArgs defaultConfig args tests