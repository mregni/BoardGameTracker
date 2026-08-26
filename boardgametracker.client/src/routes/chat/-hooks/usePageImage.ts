import { useEffect, useState } from "react";
import { getManualPageImageCall } from "@/services/manualService";

export type PageImageStatus = "loading" | "ready" | "error";

// Cache the fetched blob per URL so a page shown as both a thumbnail and the
// large view (or re-focused later) is only downloaded once. A failed fetch is
// evicted so a transient error can be retried on the next mount.
const blobCache = new Map<string, Promise<Blob>>();

const fetchBlob = (url: string): Promise<Blob> => {
	let promise = blobCache.get(url);
	if (!promise) {
		promise = getManualPageImageCall(url).catch((error) => {
			blobCache.delete(url);
			throw error;
		});
		blobCache.set(url, promise);
	}
	return promise;
};

export const usePageImage = (url: string | null) => {
	const [objectUrl, setObjectUrl] = useState<string | null>(null);
	const [status, setStatus] = useState<PageImageStatus>(url ? "loading" : "error");

	useEffect(() => {
		if (!url) {
			setObjectUrl(null);
			setStatus("error");
			return;
		}

		let cancelled = false;
		let created: string | null = null;
		setObjectUrl(null);
		setStatus("loading");

		fetchBlob(url)
			.then((blob) => {
				if (cancelled) {
					return;
				}
				created = URL.createObjectURL(blob);
				setObjectUrl(created);
				setStatus("ready");
			})
			.catch(() => {
				if (!cancelled) {
					setStatus("error");
				}
			});

		return () => {
			cancelled = true;
			if (created) {
				URL.revokeObjectURL(created);
			}
		};
	}, [url]);

	return { objectUrl, status };
};
