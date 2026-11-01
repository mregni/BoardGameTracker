import { useEffect, useRef, useState } from "react";
import { useDebounce } from "@/hooks/useDebounce";

export const useDebouncedSearchQuery = (query: string | undefined, onCommit: (query: string | undefined) => void) => {
	const [value, setValue] = useState(query ?? "");
	const debouncedValue = useDebounce(value, 300);
	const committedRef = useRef(query);
	const onCommitRef = useRef(onCommit);
	onCommitRef.current = onCommit;

	useEffect(() => {
		if (query === committedRef.current) return;
		committedRef.current = query;
		setValue(query ?? "");
	}, [query]);

	useEffect(() => {
		const next = debouncedValue.trim().length > 0 ? debouncedValue : undefined;
		if (next === committedRef.current) return;
		committedRef.current = next;
		onCommitRef.current(next);
	}, [debouncedValue]);

	return [value, setValue] as const;
};
