// Version 21 metadata layout reference:
// Perfare/Il2CppDumper 4741d46ba9cd6159c5d853eb9d6fc48b4bfa2b1a,
// Il2CppDumper/Il2Cpp/MetadataClass.cs. No native type/code registration is read.
const sections = ['stringLiterals', 'stringLiteralData', 'strings', 'events', 'properties', 'methods',
  'parameterDefaultValues', 'fieldDefaultValues', 'defaultValueData', 'fieldMarshaledSizes', 'parameters',
  'fields', 'genericParameters', 'genericParameterConstraints', 'genericContainers', 'nestedTypes',
  'interfaces', 'vtableMethods', 'interfaceOffsets', 'typeDefinitions', 'rgctxEntries', 'images',
  'assemblies', 'metadataUsageLists', 'metadataUsagePairs', 'fieldRefs', 'referencedAssemblies',
  'attributesInfo', 'attributeTypes'];
const widths = {typeDefinitions: 120, fields: 16, fieldDefaultValues: 12, images: 24};
const need = (condition, message) => { if (!condition) throw Error('IL2CPP v21: ' + message); };

export function readV21Int32Enums(bytes, families) {
  need(Buffer.isBuffer(bytes) && bytes.length >= 240, 'truncated header');
  need(bytes.readUInt32LE(0) === 0xfab11baf && bytes.readInt32LE(4) === 21, 'unsupported magic/version');
  need(new Set(families).size === families.length && families.length > 0, 'invalid family selection');
  const table = Object.fromEntries(sections.map((name, index) => {
    const offset = bytes.readInt32LE(8 + index * 8), size = bytes.readInt32LE(12 + index * 8);
    need(offset >= 240 && size >= 0 && offset + size <= bytes.length, 'section bounds: ' + name);
    if (widths[name]) need(size % widths[name] === 0, 'record size: ' + name);
    return [name, {offset, size}];
  }));
  need(table.stringLiterals.offset === 240, 'unexpected header size');
  const occupied = Object.values(table).filter(x => x.size).sort((a, b) => a.offset - b.offset);
  for (let i = 1; i < occupied.length; i++) need(occupied[i].offset >= occupied[i - 1].offset + occupied[i - 1].size, 'overlapping sections');
  const utf8 = new TextDecoder('utf-8', {fatal: true});
  const stringAt = index => {
    const {offset, size} = table.strings;
    need(index >= 0 && index < size, 'string index');
    const end = bytes.indexOf(0, offset + index);
    need(end >= offset + index && end < offset + size, 'unterminated string');
    return utf8.decode(bytes.subarray(offset + index, end));
  };
  const count = name => table[name].size / widths[name];
  const fieldOwners = new Int32Array(count('fields')).fill(-1);
  const types = Array.from({length: count('typeDefinitions')}, (_, index) => {
    const offset = table.typeDefinitions.offset + index * 120;
    const start = bytes.readInt32LE(offset + 64), length = bytes.readUInt16LE(offset + 100);
    need((length === 0 && start === -1) || (start >= 0 && start + length <= fieldOwners.length), 'type field range');
    for (let field = start; field < start + length; field++) {
      need(fieldOwners[field] === -1, 'overlapping field owners'); fieldOwners[field] = index;
    }
    need(bytes.readUInt32LE(offset + 116) >>> 24 === 2, 'type token');
    return {index, offset, name: stringAt(bytes.readInt32LE(offset)), namespace: stringAt(bytes.readInt32LE(offset + 4)),
      byval: bytes.readInt32LE(offset + 12), element: bytes.readInt32LE(offset + 28), start, length,
      bitfield: bytes.readUInt32LE(offset + 112)};
  });
  need(fieldOwners.every(x => x >= 0), 'unowned field');
  const imageOwners = new Int32Array(types.length).fill(-1);
  const images = Array.from({length: count('images')}, (_, index) => {
    const offset = table.images.offset + index * 24;
    const start = bytes.readInt32LE(offset + 8), length = bytes.readUInt32LE(offset + 12);
    need(start >= 0 && start + length <= types.length, 'image type range');
    for (let type = start; type < start + length; type++) {
      need(imageOwners[type] === -1, 'overlapping image owners'); imageOwners[type] = index;
    }
    return {name: stringAt(bytes.readInt32LE(offset)), start, count: length};
  });
  need(imageOwners.every(x => x >= 0), 'unowned type');
  const int32Types = types.filter(x => x.namespace === 'System' && x.name === 'Int32');
  need(int32Types.length === 1 && images[imageOwners[int32Types[0].index]].name === 'mscorlib.dll', 'ambiguous System.Int32');
  const defaults = new Map();
  for (let i = 0; i < count('fieldDefaultValues'); i++) {
    const offset = table.fieldDefaultValues.offset + i * 12;
    const field = bytes.readInt32LE(offset), type = bytes.readInt32LE(offset + 4), data = bytes.readInt32LE(offset + 8);
    need(field >= 0 && field < fieldOwners.length && !defaults.has(field), 'invalid/duplicate field default');
    need(data >= -1 && data < table.defaultValueData.size, 'default data index');
    defaults.set(field, {offset, type, data});
  }
  const result = families.map(name => {
    const matches = types.filter(t => t.name === name && t.namespace === '' && images[imageOwners[t.index]].name === 'Assembly-CSharp.dll');
    need(matches.length === 1, 'missing/ambiguous family: ' + name);
    const t = matches[0];
    need((t.bitfield & 3) === 3 && t.element === int32Types[0].byval && t.length > 1, 'not an Int32 enum: ' + name);
    const values = [], excludedSentinels = [], labels = new Set();
    for (let index = t.start; index < t.start + t.length; index++) {
      const fieldOffset = table.fields.offset + index * 16;
      const label = stringAt(bytes.readInt32LE(fieldOffset));
      need(!labels.has(label) && bytes.readUInt32LE(fieldOffset + 12) >>> 24 === 4, 'invalid field label/token'); labels.add(label);
      const d = defaults.get(index);
      if (index === t.start) { need(label === 'value__' && d === undefined, 'enum backing field'); continue; }
      need(/^[A-Za-z_][A-Za-z0-9_]*$/.test(label) && d && d.type === t.element && d.data >= 0 && d.data + 4 <= table.defaultValueData.size, 'invalid Int32 constant: ' + name + '.' + label);
      const dataOffset = table.defaultValueData.offset + d.data;
      const value = {name: label, value: bytes.readInt32LE(dataOffset), fieldIndex: index, fieldOffset, defaultRecordOffset: d.offset, dataOffset};
      if (label === 'sizeof_' + name) excludedSentinels.push(value); else values.push(value);
    }
    return {name, image: 'Assembly-CSharp.dll', typeIndex: t.index, typeOffset: t.offset, values, excludedSentinels};
  });
  return {metadataVersion: 21, sections: table, counts: {types: types.length, fields: fieldOwners.length, fieldDefaults: defaults.size, images: images.length}, families: result};
}
