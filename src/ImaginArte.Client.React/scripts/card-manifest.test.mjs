import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { URL } from 'node:url';

const manifestUrl = new URL('../public/cards/manifest.v1.json', import.meta.url);
const manifest = JSON.parse(readFileSync(manifestUrl, 'utf8'));
const expectedCardIds = Array.from(
  { length: 98 },
  (_, index) => `TEMP-${String(index + 1).padStart(3, '0')}`,
);

test('manifest declares schema version 1', () => {
  assert.equal(manifest.schemaVersion, 1);
});

test('manifest contains exactly the 98 unique temporary card IDs and files', () => {
  assert.equal(manifest.cards.length, 98);

  const cardIds = manifest.cards.map((card) => card.cardId);
  const fileNames = manifest.cards.map((card) => card.fileName);

  assert.deepEqual(cardIds, expectedCardIds);
  assert.equal(new Set(cardIds).size, manifest.cards.length);
  assert.equal(new Set(fileNames).size, manifest.cards.length);
});

test('each card ID ordinal matches the JPG filename prefix', () => {
  for (const card of manifest.cards) {
    const cardIdMatch = /^TEMP-(\d{3})$/.exec(card.cardId);
    const fileNameMatch = /^(\d{2})-.+\.jpg$/.exec(card.fileName);

    assert.ok(cardIdMatch, `${card.cardId} must use the TEMP-NNN format`);
    assert.ok(fileNameMatch, `${card.fileName} must start with a two-digit ordinal`);
    assert.equal(Number(cardIdMatch[1]), Number(fileNameMatch[1]));
    assert.deepEqual(Object.keys(card).sort(), ['alt', 'cardId', 'fileName']);
  }
});

test('every card has a nonempty accessible description', () => {
  for (const card of manifest.cards) {
    assert.equal(typeof card.alt, 'string');
    assert.notEqual(card.alt.trim(), '');
  }
});
