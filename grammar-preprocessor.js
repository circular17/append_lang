// grammar preprocessor to generate actual PEG input file

const fs = require('fs');
const customPeg = require("./generated/custom-peg");

const inputFile = process.argv[2]
const outputFile = process.argv[3]

before = fs.readFileSync(inputFile, "utf8")
grammarCode = customPeg.parse(before)
after = grammarCode.join("")
fs.writeFileSync(outputFile, after, "utf8")
