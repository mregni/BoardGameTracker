import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { GameState } from "@/models";
import { getColorFromGameState, getItemStateTranslationKey } from "../../utils/ItemStateUtils";
import { BgtFallbackImage } from "../BgtImage/BgtFallbackImage";
import { BgtText } from "../BgtText/BgtText";

interface Props {
	title: string;
	state?: GameState;
	image: string | null;
	link: string;
	isLoaned?: boolean;
	badge?: ReactNode;
	meta?: ReactNode;
}

export const BgtImageCard = (props: Props) => {
	const { title, image, state, link, isLoaned = false, badge, meta } = props;
	const { t } = useTranslation();
	const showState = state !== null && state !== undefined && (isLoaned || state !== GameState.Owned);

	return (
		<Link
			to={link}
			from="/"
			className="bgt-image-card row-span-3 grid grid-rows-subgrid gap-y-1 rounded-lg cursor-pointer"
		>
			<div className="aspect-square rounded-lg overflow-hidden relative bg-card-light ring-1 ring-inset ring-white/5">
				<BgtFallbackImage title={title} image={image} variant="cover" decorative lazy />
				{showState && (
					<BgtText
						size="1"
						weight="medium"
						color={getColorFromGameState(state, isLoaned)}
						className="absolute top-2 left-2 max-w-[calc(100%-3.5rem)] truncate rounded-full bg-black/75 px-2 py-0.5"
					>
						{t(getItemStateTranslationKey(state, isLoaned))}
					</BgtText>
				)}
				{badge && <div className="absolute top-2 right-2">{badge}</div>}
			</div>
			<BgtText size="3" className="mt-1 line-clamp-2 w-full leading-snug" weight="medium">
				{title}
			</BgtText>
			<div className="min-w-0">{meta}</div>
		</Link>
	);
};
