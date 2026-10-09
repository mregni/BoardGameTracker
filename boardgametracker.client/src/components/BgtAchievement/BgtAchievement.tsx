import * as Tooltip from "@radix-ui/react-tooltip";
import { useTranslation } from "react-i18next";
import type { Badge } from "@/models";

interface Props {
	badge: Badge;
}

export const BgtAchievementIcon = (props: Props) => {
	const { badge } = props;
	const { t } = useTranslation("badges");

	return (
		<Tooltip.Provider>
			<Tooltip.Root>
				<Tooltip.Trigger asChild>
					<img src={`/images/badges/${badge.image}`} alt={t(badge.titleKey)} className="h-10 aspect-square" />
				</Tooltip.Trigger>
				<Tooltip.Portal>
					<Tooltip.Content
						className="select-none rounded-sm bg-card-black border-card-border border-2 border-solid px-[15px] py-2.5 text-[15px] leading-none will-change-[transform,opacity] data-[state=delayed-open]:data-[side=bottom]:animate-slide-up-and-fade data-[state=delayed-open]:data-[side=left]:animate-slide-right-and-fade data-[state=delayed-open]:data-[side=right]:animate-slide-left-and-fade data-[state=delayed-open]:data-[side=top]:animate-slide-down-and-fade"
						sideOffset={5}
					>
						<div className="flex flex-col justify-center">
							<div className="font-bold">{t(`${badge.titleKey}`)}</div>
							<div className="text-xs">{t(`${badge.descriptionKey}`)}</div>
						</div>
					</Tooltip.Content>
				</Tooltip.Portal>
			</Tooltip.Root>
		</Tooltip.Provider>
	);
};
