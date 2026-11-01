import { cx } from "class-variance-authority";
import { type CSSProperties, useState } from "react";
import { StringToRgb } from "@/utils/stringUtils";

interface Props {
	title: string;
	image: string | null | undefined;
	className?: string;
	lazy?: boolean;
	variant?: "initial" | "cover";
	decorative?: boolean;
}

export const BgtFallbackImage = ({
	title,
	image,
	className,
	lazy = false,
	variant = "initial",
	decorative = false,
}: Props) => {
	const [failedImage, setFailedImage] = useState<string | null>(null);
	const showImage = !!image && failedImage !== image;

	if (showImage) {
		return (
			<img
				src={image}
				alt={decorative ? "" : title}
				loading={lazy ? "lazy" : undefined}
				decoding="async"
				className={cx("w-full h-full object-cover", className)}
				onError={() => setFailedImage(image)}
			/>
		);
	}

	if (variant === "cover") {
		return (
			<div
				{...(decorative ? { "aria-hidden": true } : { role: "img", "aria-label": title })}
				style={{ "--fallback-color": StringToRgb(title) } as CSSProperties}
				className={cx(
					"w-full h-full flex items-end p-3 bg-(--fallback-color)",
					"bg-[linear-gradient(to_bottom,transparent_35%,rgb(0_0_0/0.55)),linear-gradient(150deg,rgb(255_255_255/0.18),transparent_55%)]",
					className,
				)}
			>
				<span aria-hidden="true" className="line-clamp-3 text-xl font-bold leading-tight text-white text-balance">
					{title}
				</span>
			</div>
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
