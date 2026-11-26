// Run with node Contracts/check-game-data.js. No third-party packages required.
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const schema = JSON.parse(fs.readFileSync(path.join(__dirname, 'game-data-v1.schema.json'), 'utf8'));
const models = [
  ['Map', 'Assets/Scripts/BitzData/Models/BitzMap/BitzMap.cs', 'BitzMap'],
  ['PlayerInfo', 'Assets/Scripts/BitzData/Models/GameData/PlayerData.cs', 'PlayerInfo'],
  ['PlayerSetting', 'Assets/Scripts/BitzData/Models/GameData/PlayerData.cs', 'PlayerSetting'],
  ['MapScore', 'Assets/Scripts/BitzData/Models/BitzMap/BitzMapScore.cs', 'BitzMapScore'],
];
let failed = false;
for (const [definition, file, className] of models) {
  const source = fs.readFileSync(path.join(root, file), 'utf8');
  const start = source.search(new RegExp('\\bclass\\s+' + className + '\\b'));
  if (start < 0) throw new Error(`Missing ${className}`);
  const next = source.slice(start + 1).search(/\b(?:public\s+)?class\s+\w+/);
  const body = next < 0 ? source.slice(start) : source.slice(start, start + 1 + next);
  const actual = {};
  const pattern = /\[([^\]]*(?:Column|PrimaryKey)\("[^"]+"\)[^\]]*)\]\s*public\s+([\w.<>?]+)\s+\w+\s*\{/g;
  for (const match of body.matchAll(pattern)) {
    const name = /(?:Column|PrimaryKey)\("([^"]+)"\)/.exec(match[1])[1];
    const jsonName = /JsonPropertyName\("([^"]+)"\)/.exec(match[1]);
    if (jsonName && jsonName[1] !== name) { console.error(`${className}.${name}: JSON name is ${jsonName[1]}`); failed = true; }
    const type = match[2];
    if (!['Guid', 'Guid?', 'string', 'string?', 'int', 'bool'].includes(type) || Object.hasOwn(actual, name)) { console.error(`${className}.${name}: unsupported or duplicate field`); failed = true; continue; }
    actual[name] = type === 'Guid' ? {type:'string',format:'uuid'} : type === 'Guid?' ? {type:['string','null'],format:'uuid'} : type.startsWith('string') ? {type:['string','null']} : type === 'int' ? {type:'integer',minimum:-2147483648,maximum:2147483647} : {type:'boolean'};
  }
  const def = schema.$defs[definition];
  const sorted = o => JSON.stringify(Object.keys(o).sort().map(k => [k, o[k]]));
  if (sorted(actual) !== sorted(def.properties) || JSON.stringify(Object.keys(actual).sort()) !== JSON.stringify([...def.required].sort())) {
    console.error(`${className}: model fields or types differ from ${definition} contract`);
    failed = true;
  } else console.log(`${className}: matches ${definition}`);
}
if (failed) process.exitCode = 1;
