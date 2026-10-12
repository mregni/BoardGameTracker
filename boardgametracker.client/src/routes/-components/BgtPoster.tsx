import { cx } from "class-variance-authority";
import type { ComponentPropsWithoutRef } from "react";

import { BgtFallbackImage } from "@/components/BgtImage/BgtFallbackImage";

interface Props extends ComponentPropsWithoutRef<"div"> {
	title: string;
	image: string | null;
}

export const BgtPoster = (props: Props) => {
	const { className, title, image } = props;

	return (
		<div className={cx(className, "relative overflow-hidden aspect-square rounded-xl w-full")}>
			<BgtFallbackImage title={title} image={image} />
		</div>
	);
};
