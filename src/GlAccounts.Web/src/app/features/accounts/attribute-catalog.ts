// The contract (openapi.yaml) has no attribute-catalog operation, so the UI carries a copy of the stub catalog
// (InMemoryAttributeCatalogStore). Stub data only. The API remains the authority: it rejects any type/value
// pair that is not in its catalog, and the panel shows that error on the row (AC-GLA-016).

export interface CatalogValue {
  id: string;
  label: string;
}

export interface CatalogType {
  id: string;
  label: string;
  values: CatalogValue[];
}

export const ATTRIBUTE_CATALOG: readonly CatalogType[] = [
  {
    id: 'dept',
    label: 'Department',
    values: [
      { id: 'finance', label: 'Finance' },
      { id: 'sales', label: 'Sales' },
      { id: 'engineering', label: 'Engineering' },
    ],
  },
  {
    id: 'region',
    label: 'Region',
    values: [
      { id: 'emea', label: 'EMEA' },
      { id: 'na', label: 'North America' },
      { id: 'apac', label: 'APAC' },
    ],
  },
  {
    id: 'project',
    label: 'Project',
    values: [
      { id: 'proj-alpha', label: 'Alpha' },
      { id: 'proj-beta', label: 'Beta' },
    ],
  },
];

export function typeLabel(typeId: string): string {
  return ATTRIBUTE_CATALOG.find((t) => t.id === typeId)?.label ?? typeId;
}

export function valueLabel(typeId: string, valueId: string): string {
  return ATTRIBUTE_CATALOG.find((t) => t.id === typeId)?.values.find((v) => v.id === valueId)?.label ?? valueId;
}

export function valuesFor(typeId: string): readonly CatalogValue[] {
  return ATTRIBUTE_CATALOG.find((t) => t.id === typeId)?.values ?? [];
}
