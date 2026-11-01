import { cva, cx, type VariantProps } from "class-variance-authority";
import { useState } from "react";
import { StringToHsl } from "@/utils/stringUtils";
import { textSizeClasses } from "../BgtText/textStyles";

const avatarVariants = cva("shadow-gray-800 shadow-md", {
	variants: {
		size: {
			small: "h-5 w-5",
			medium: "h-7 w-7",
			large: "h-11 w-11",
			big: "h-20 w-20 md:h-28 md:w-28",
		},
		shape: {
			square: "",
			round: "rounded-full",
		},
		interactive: {
			true: "hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer",
			false: "",
		},
		disabled: {
			true: "opacity-50",
			false: "",
		},
		hasImage: {
			true: "cursor-pointer",
			false: "cursor-pointer flex justify-center items-center",
		},
	},
	compoundVariants: [
		{ shape: "square", size: "small", class: "rounded-xs" },
		{ shape: "square", size: "medium", class: "rounded-md" },
		{ shape: "square", size: "large", class: "rounded-lg" },
		{ shape: "square", size: "big", class: "rounded-full" },
	],
	defaultVariants: {
		shape: "square",
		size: "medium",
		interactive: false,
		disabled: false,
		hasImage: true,
	},
});

export interface Props extends Omit<VariantProps<typeof avatarVariants>, "interactive" | "hasImage"> {
	title?: string;
	colorKey?: string;
	color?: string;
	image: string | undefined | null;
	onClick?: () => void;
	withTitle?: boolean;
}

const TEXT_SIZE_MAP: Record<string, "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"> = {
	big: "8",
	large: "5",
	medium: "3",
	small: "2",
};

export const BgtAvatar = (props: Props) => {
	const { title, colorKey, color, image, onClick, size, shape, disabled, withTitle = false } = props;
	const [failedImage, setFailedImage] = useState<string | null>(null);
	const showImage = !!image && failedImage !== image;

	if (!image && !title) return null;

	const avatarClasses = avatarVariants({
		size,
		shape,
		interactive: !!onClick,
		disabled,
		hasImage: showImage,
	});

	const textSize = TEXT_SIZE_MAP[size || "medium"];

	return (
		<div
			className={cx("group flex relative min-w-7 flex-row- gap-2 items-center", onClick && "cursor-pointer")}
			onClick={onClick}
			onKeyDown={
				onClick
					? (e: React.KeyboardEvent) => {
							if (e.key === "Enter" || e.key === " ") {
								e.preventDefault();
								onClick();
							}
						}
					: undefined
			}
			role={onClick ? "button" : undefined}
			tabIndex={onClick ? 0 : undefined}
			title={title}
		>
			{showImage && (
				<img
					className={cx(avatarClasses, "bg-white/5")}
					style={title ? { backgroundColor: StringToHsl(colorKey ?? title) } : undefined}
					src={image}
					alt={title || ""}
					loading="lazy"
					onError={() => setFailedImage(image ?? null)}
				/>
			)}
			{!showImage && title && (
				<div
					style={{ backgroundColor: color ?? StringToHsl(colorKey ?? title) }}
					className={avatarClasses}
					aria-hidden={onClick ? true : undefined}
				>
					<span className={cx(textSizeClasses[textSize], "capitalize")}>{title[0]}</span>
				</div>
			)}
			{withTitle && title && <span className={cx(onClick && "cursor-pointer")}>{title}</span>}
		</div>
	);
};
