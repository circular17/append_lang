// grammar preprocessor to generate actual PEG input file

import fs from 'fs'
import * as customPeg from "./generated/custom-peg.mjs"

const inputFile = process.argv[2]
const outputFile = process.argv[3]

const before = fs.readFileSync(inputFile, "utf8")
const grammarCode = customPeg.parse(before)
const after = grammarCode.join("")
fs.writeFileSync(outputFile, after, "utf8")
