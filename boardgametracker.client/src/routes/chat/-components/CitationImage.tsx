import { useEffect, useState } from "react";
import { getManualPageImageCall } from "@/services/manualService";

interface Props {
	url: string;
	alt: string;
}

export const CitationImage = ({ url, alt }: Props) => {
	const [objectUrl, setObjectUrl] = useState<string | null>(null);

	useEffect(() => {
		let created: string | null = null;
		let cancelled = false;

		getManualPageImageCall(url)
			.then((blob) => {
				if (cancelled) {
					return;
				}
				created = URL.createObjectURL(blob);
				setObjectUrl(created);
			})
			.catch(() => {
				// Page images are optional; ignore failures (e.g. the renderer is unavailable).
			});

		return () => {
			cancelled = true;
			if (created) {
				URL.revokeObjectURL(created);
			}
		};
	}, [url]);

	if (!objectUrl) {
		return null;
	}

	return <img src={objectUrl} alt={alt} className="mt-2 max-h-64 rounded-md border border-white/10" />;
};
