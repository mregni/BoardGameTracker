import { useMemo, useState } from "react";

import { useDebounce } from "./useDebounce";

export function useFilteredList<T>(items: T[], searchKey: keyof T, preFilter?: (items: T[]) => T[], delay = 300) {
	const [filterValue, setFilterValue] = useState("");
	const debouncedFilterValue = useDebounce(filterValue, delay);

	const filtered = useMemo(() => {
		const base = preFilter ? preFilter(items) : items;

		if (!debouncedFilterValue) return base;

		const lower = debouncedFilterValue.toLowerCase();
		return base.filter((item) => String(item[searchKey]).toLowerCase().includes(lower));
	}, [items, debouncedFilterValue, searchKey, preFilter]);

	return { filterValue, setFilterValue, filtered };
}
