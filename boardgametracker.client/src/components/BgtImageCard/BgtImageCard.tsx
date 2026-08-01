import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import type { GameState } from "@/models";
import { StringToRgb } from "@/utils/stringUtils";
import { getColorFromGameState, getItemStateTranslationKey } from "../../utils/ItemStateUtils";
import { BgtText } from "../BgtText/BgtText";

interface Props {
	title: string;
	state?: GameState;
	image: string | null;
	link: string;
	isLoaned?: boolean;
}

export const BgtImageCard = (props: Props) => {
	const { title, image, state, link, isLoaned = false } = props;
	const { t } = useTranslation();

	return (
		<Link to={link} from="/" className="[content-visibility:auto] [contain-intrinsic-size:auto_280px]">
			<div className="flex flex-col justify-center cursor-pointer flex-nowrap relative gap-1 group">
				<div className="aspect-square rounded-lg overflow-hidden transition-all duration-200 relative">
					{image ? (
						<img
							src={image}
							alt={title}
							loading="lazy"
							decoding="async"
							className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
						/>
					) : (
						<div
							style={{ "--fallback-color": StringToRgb(title) } as React.CSSProperties}
							className="w-full h-full flex flex-col justify-center bg-(--fallback-color)"
						>
							<span className="flex justify-center align-middle h-max font-bold text-3xl capitalize">{title[0]}</span>
						</div>
					)}
				</div>
				<div className="flex flex-row justify-between items-end">
					<div className="flex flex-col items-start justify-start">
						{state !== null && state !== undefined && (
							<BgtText
								size="1"
								className="line-clamp-1 w-full"
								weight="medium"
								color={getColorFromGameState(state, isLoaned)}
							>
								{t(getItemStateTranslationKey(state, isLoaned))}
							</BgtText>
						)}
						<BgtText size="4" className="line-clamp-1 w-full" weight="medium">
							{title}
						</BgtText>
					</div>
				</div>
			</div>
		</Link>
	);
};
