import { cx } from "class-variance-authority";
import { type CSSProperties, useState } from "react";
import { StringToRgb } from "@/utils/stringUtils";

interface Props {
	title: string;
	image: string | null | undefined;
	className?: string;
	lazy?: boolean;
	decorative?: boolean;
}

export const BgtFallbackImage = ({ title, image, className, lazy = false, decorative = false }: Props) => {
	const [failedImage, setFailedImage] = useState<string | null>(null);
	const [loadedImage, setLoadedImage] = useState<string | null>(null);
	const showImage = !!image && failedImage !== image;

	if (showImage) {
		return (
			<img
				ref={(node) => {
					if (node?.complete && node.naturalWidth > 0) setLoadedImage(image);
				}}
				src={image}
				alt={decorative ? "" : title}
				loading={lazy ? "lazy" : undefined}
				decoding="async"
				className={cx(
					"w-full h-full object-cover transition-[opacity,filter] duration-500 ease-out",
					loadedImage !== image && "opacity-0 motion-safe:blur-md",
					className,
				)}
				onLoad={() => setLoadedImage(image)}
				onError={() => setFailedImage(image)}
			/>
		);
	}

	return (
		<div
			{...(decorative ? { "aria-hidden": true } : { role: "img", "aria-label": title })}
			style={{ "--fallback-color": StringToRgb(title) } as CSSProperties}
			className={cx("w-full h-full flex flex-col justify-center bg-(--fallback-color)", className)}
		>
			<span className="flex justify-center align-middle h-max font-bold text-3xl capitalize">{title[0]}</span>
		</div>
	);
};
