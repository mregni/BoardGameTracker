import { cx } from "class-variance-authority";
import { type CSSProperties, useState } from "react";
import { StringToRgb } from "@/utils/stringUtils";

interface Props {
	title: string;
	image: string | null | undefined;
	className?: string;
	lazy?: boolean;
}

export const BgtFallbackImage = ({ title, image, className, lazy = false }: Props) => {
	const [failedImage, setFailedImage] = useState<string | null>(null);
	const showImage = !!image && failedImage !== image;

	if (showImage) {
		return (
			<img
				src={image}
				alt={title}
				loading={lazy ? "lazy" : undefined}
				decoding="async"
				className={cx("w-full h-full object-cover", className)}
				onError={() => setFailedImage(image)}
			/>
		);
	}

	return (
		<div
			role="img"
			aria-label={title}
			style={{ "--fallback-color": StringToRgb(title) } as CSSProperties}
			className={cx("w-full h-full flex flex-col justify-center bg-(--fallback-color)", className)}
		>
			<span className="flex justify-center align-middle h-max font-bold text-3xl capitalize">{title[0]}</span>
		</div>
	);
};
