import { useEffect } from "react";

export const useDocumentTitle = (title: string | undefined) => {
	useEffect(() => {
		if (!title) return;
		const previous = document.title;
		document.title = `${title} · BoardGameTracker`;
		return () => {
			document.title = previous;
		};
	}, [title]);
};
