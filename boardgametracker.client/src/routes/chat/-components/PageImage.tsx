import type { ReactEventHandler, ReactNode } from "react";
import { usePageImage } from "../-hooks/usePageImage";

interface Props {
	url: string | null;
	alt: string;
	className?: string;
	fallback?: ReactNode;
	onLoad?: ReactEventHandler<HTMLImageElement>;
}

export const PageImage = ({ url, alt, className, fallback = null, onLoad }: Props) => {
	const { objectUrl, status } = usePageImage(url);

	if (status === "ready" && objectUrl) {
		return <img src={objectUrl} alt={alt} className={className} onLoad={onLoad} />;
	}

	return <>{fallback}</>;
};
