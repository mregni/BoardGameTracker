import type { MouseEventHandler, ReactEventHandler, ReactNode } from "react";
import { usePageImage } from "../-hooks/usePageImage";

interface Props {
	url: string | null;
	alt: string;
	className?: string;
	fallback?: ReactNode;
	onClick?: MouseEventHandler<HTMLImageElement>;
	onLoad?: ReactEventHandler<HTMLImageElement>;
}

export const PageImage = ({ url, alt, className, fallback = null, onClick, onLoad }: Props) => {
	const { objectUrl, status } = usePageImage(url);

	if (status === "ready" && objectUrl) {
		return <img src={objectUrl} alt={alt} className={className} onClick={onClick} onLoad={onLoad} />;
	}

	return <>{fallback}</>;
};
