import * as Tooltip from "@radix-ui/react-tooltip";
import { differenceInDays } from "date-fns";
import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import type { Badge } from "@/models";

interface Props {
	badge: Badge;
}

const RECENT_DAYS = 7;

export const BgtAchievementIcon = (props: Props) => {
	const { badge } = props;
	const { t } = useTranslation("badges");
	const [open, setOpen] = useState(false);
	const openAtPress = useRef<boolean | null>(null);
	const [isRecent] = useState(
		() => !!badge.awardedAt && differenceInDays(new Date(), new Date(badge.awardedAt)) < RECENT_DAYS,
	);
	const imageUrl = `/images/badges/${badge.image}`;

	return (
		<Tooltip.Provider>
			<Tooltip.Root open={open} onOpenChange={setOpen}>
				<Tooltip.Trigger asChild>
					<button
						type="button"
						className="relative block overflow-hidden rounded-md focus-visible:outline-2 focus-visible:outline-primary"
						onPointerDown={() => {
							openAtPress.current = open;
						}}
						onClick={(event) => {
							event.preventDefault();
							const wasOpen = openAtPress.current ?? open;
							openAtPress.current = null;
							setOpen(!wasOpen);
						}}
					>
						<img src={imageUrl} alt={t(badge.titleKey)} className="h-10 aspect-square" />
						{isRecent && (
							<span
								aria-hidden="true"
								data-testid="badge-shine"
								className="pointer-events-none absolute inset-0 mask-contain mask-center mask-no-repeat motion-reduce:hidden"
								style={{ maskImage: `url(${imageUrl})` }}
							>
								<span className="absolute inset-0 bg-linear-to-r from-transparent via-white/70 to-transparent motion-safe:animate-badge-shine" />
							</span>
						)}
					</button>
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
