import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useEffect, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtFallbackImage } from "@/components/BgtImage/BgtFallbackImage";
import { useDurationRange } from "@/hooks/useDurationRange";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { usePermissions } from "@/hooks/usePermissions";
import type { Game } from "@/models";
import { formatRange } from "@/utils/rangeUtils";

interface Props {
	games: Game[];
	winnerId?: number;
	onSpinStart: () => void;
	onResult: (gameId: number) => void;
	onExclude: (gameId: number) => void;
	weight?: (game: Game) => number;
}

export const PICK_ACTION_SELECTOR = "[data-pick-action]";

const RADIUS = 96;
const HUB = 16;
const FULL_TURNS = 6;
const SPIN_MS = 4200;
const VETO_MS = 320;

const point = (angle: number, radius: number) => {
	const radians = (angle * Math.PI) / 180;
	return { x: radius * Math.sin(radians), y: -radius * Math.cos(radians) };
};

const segmentPath = (start: number, end: number) => {
	if (end - start >= 360) {
		return `M 0 ${-RADIUS} A ${RADIUS} ${RADIUS} 0 1 1 0 ${RADIUS} A ${RADIUS} ${RADIUS} 0 1 1 0 ${-RADIUS} Z`;
	}
	const from = point(start, RADIUS);
	const to = point(end, RADIUS);
	return `M 0 0 L ${from.x} ${from.y} A ${RADIUS} ${RADIUS} 0 ${end - start > 180 ? 1 : 0} 1 ${to.x} ${to.y} Z`;
};

const segmentCoverBox = (start: number, end: number) => {
	const corners = [{ x: 0, y: 0 }, point(start, RADIUS), point(end, RADIUS)];
	for (let angle = 0; angle <= 360; angle += 90) {
		if (angle > start && angle < end) corners.push(point(angle, RADIUS));
	}
	const xs = corners.map((corner) => corner.x);
	const ys = corners.map((corner) => corner.y);
	const size = Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys));
	return {
		x: (Math.max(...xs) + Math.min(...xs)) / 2 - size / 2,
		y: (Math.max(...ys) + Math.min(...ys)) / 2 - size / 2,
		size,
	};
};

const normalise = (angle: number) => ((angle % 360) + 360) % 360;

export const PickWheel = ({ games, winnerId, onSpinStart, onResult, onExclude, weight }: Props) => {
	const { t } = useTranslation("games");
	const navigate = useNavigate();
	const { canWrite } = usePermissions();
	const durationRange = useDurationRange();
	const clipPrefix = useId();
	const reduceMotion = useMediaQuery("(prefers-reduced-motion: reduce)");
	const wheelRef = useRef<SVGGElement>(null);
	const cardRef = useRef<HTMLDivElement>(null);
	const weights = games.map((game) => weight?.(game) ?? 1);
	const totalWeight = weights.reduce((sum, value) => sum + value, 0);
	const sizes = weights.map((value) => (360 * value) / totalWeight);
	const starts = sizes.map((_, index) => sizes.slice(0, index).reduce((sum, value) => sum + value, 0));
	const restingRotation = (index: number) => 360 - (starts[index] + sizes[index] / 2);
	const initialIndex = games.findIndex((game) => game.id === winnerId);
	const rotationRef = useRef(restingRotation(Math.max(initialIndex, 0)));
	const spunRef = useRef(false);
	const respinRef = useRef(false);
	const [spinning, setSpinning] = useState(false);
	const [vetoId, setVetoId] = useState<number>();
	const [vetoedTitle, setVetoedTitle] = useState<string>();

	const winnerIndex = spinning ? -1 : games.length === 1 ? 0 : games.findIndex((game) => game.id === winnerId);
	const picked = winnerIndex >= 0 ? games[winnerIndex] : undefined;

	useEffect(() => {
		if (!spunRef.current) return;
		if (picked) {
			cardRef.current?.scrollIntoView?.({ block: "end", behavior: reduceMotion ? "auto" : "smooth" });
		}
		document.querySelector<HTMLElement>(PICK_ACTION_SELECTOR)?.focus({ preventScroll: true });
	}, [picked, reduceMotion]);

	const spin = () => {
		const wheel = wheelRef.current;
		if (!wheel || spinning || games.length < 2) return;
		let draw = Math.random() * totalWeight;
		const next = Math.max(
			weights.findIndex((value) => {
				draw -= value;
				return draw < 0;
			}),
			0,
		);
		const from = rotationRef.current;
		const to = from - (from % 360) + FULL_TURNS * 360 + restingRotation(next);
		rotationRef.current = to;
		spunRef.current = true;
		onSpinStart();
		if (typeof wheel.animate !== "function") {
			wheel.style.transform = `rotate(${to}deg)`;
			onResult(games[next].id);
			return;
		}
		setSpinning(true);
		const animation = wheel.animate([{ transform: `rotate(${from}deg)` }, { transform: `rotate(${to}deg)` }], {
			duration: reduceMotion ? 0 : SPIN_MS,
			easing: "cubic-bezier(0.12, 0.8, 0.18, 1)",
			fill: "forwards",
		});
		animation.onfinish = () => {
			setSpinning(false);
			onResult(games[next].id);
		};
	};

	useEffect(() => {
		if (!respinRef.current) return;
		respinRef.current = false;
		spin();
	});

	const spinAgain = () => {
		setVetoedTitle(undefined);
		spin();
	};

	const veto = (game: Game) => {
		const gameId = game.id;
		spunRef.current = true;
		const respin = games.length > 2;
		setVetoedTitle(game.title);
		setVetoId(gameId);
		window.setTimeout(
			() => {
				respinRef.current = respin;
				setVetoId(undefined);
				onExclude(gameId);
			},
			reduceMotion ? 0 : VETO_MS,
		);
	};

	const overlayClass = (index: number) => {
		if (winnerIndex < 0) return "fill-black/50";
		return index === winnerIndex ? "fill-black/10" : "fill-black/75";
	};

	const fontSizeFor = (size: number) =>
		Math.max(6.5, Math.min(9.5, 2 * RADIUS * 0.6 * Math.sin((Math.min(size, 180) / 2) * (Math.PI / 180)) * 0.42));
	const label = (title: string, fontSize: number) => {
		const maxChars = Math.floor((RADIUS - HUB - 10) / (fontSize * 0.56));
		return title.length > maxChars ? `${title.slice(0, maxChars - 1)}…` : title;
	};
	const labelDistance = (RADIUS + HUB) / 2;
	const announcement = () => {
		if (picked) return t("pick.result", { title: picked.title });
		if (!spinning) return "";
		return vetoedTitle ? t("pick.vetoed", { title: vetoedTitle }) : t("pick.spinning");
	};

	return (
		<div className="flex flex-col items-center gap-4 w-full">
			<ul className="sr-only">
				{games.map((game) => (
					<li key={game.id}>{game.title}</li>
				))}
			</ul>
			{games.length > 1 && (
				<svg
					viewBox="-112 -112 224 224"
					className={cx("w-full max-w-[480px] aspect-square", !picked && "cursor-pointer")}
					aria-hidden="true"
					onClick={picked ? undefined : spinAgain}
				>
					<g ref={wheelRef} style={{ transformOrigin: "0 0", transform: `rotate(${rotationRef.current}deg)` }}>
						{games.map((game, index) => {
							const start = starts[index];
							const end = start + sizes[index];
							const box = segmentCoverBox(start, end);
							const clipId = `${clipPrefix}-${game.id}`;
							const mid = start + sizes[index] / 2;
							const fontSize = fontSizeFor(sizes[index]);
							const upsideDown = normalise(mid + rotationRef.current) > 180;
							return (
								<g key={game.id} className={cx("wheel-slice", game.id === vetoId && "vetoed")}>
									<clipPath id={clipId}>
										<path d={segmentPath(start, end)} />
									</clipPath>
									<g clipPath={`url(#${clipId})`}>
										<rect x={-RADIUS} y={-RADIUS} width={RADIUS * 2} height={RADIUS * 2} className="fill-primary/40" />
										{game.image && (
											<image
												href={game.image}
												x={box.x}
												y={box.y}
												width={box.size}
												height={box.size}
												preserveAspectRatio="xMidYMid slice"
											/>
										)}
										<rect
											x={-RADIUS}
											y={-RADIUS}
											width={RADIUS * 2}
											height={RADIUS * 2}
											className={`${overlayClass(index)} transition-[fill] duration-500 motion-reduce:transition-none`}
										/>
									</g>
									<text
										transform={
											upsideDown
												? `rotate(${mid + 90}) translate(${-labelDistance} 0)`
												: `rotate(${mid - 90}) translate(${labelDistance} 0)`
										}
										textAnchor="middle"
										dominantBaseline="middle"
										paintOrder="stroke"
										strokeWidth={fontSize * 0.28}
										strokeLinejoin="round"
										fontSize={fontSize}
										className="fill-white stroke-black/80 font-bold"
									>
										<title>{game.title}</title>
										{label(game.title, fontSize)}
									</text>
								</g>
							);
						})}
						{games.map((game, index) => {
							const edge = point(starts[index], RADIUS);
							return (
								<line
									key={`divider-${game.id}`}
									x1={0}
									y1={0}
									x2={edge.x}
									y2={edge.y}
									className="stroke-background"
									strokeWidth={1.5}
								/>
							);
						})}
						<circle r={RADIUS + 1} fill="none" className="stroke-primary" strokeWidth={3} />
						{games.map((game, index) => {
							const peg = point(starts[index], RADIUS + 1);
							return <circle key={`peg-${game.id}`} cx={peg.x} cy={peg.y} r={1.6} className="fill-primary-text" />;
						})}
						{winnerIndex >= 0 && (
							<path
								d={segmentPath(starts[winnerIndex], starts[winnerIndex] + sizes[winnerIndex])}
								fill="none"
								className="stroke-primary-text"
								strokeWidth={1.5}
								strokeLinejoin="round"
							/>
						)}
					</g>
					<circle r={HUB} className="fill-card stroke-primary" strokeWidth={2.5} />
					<path
						d="M -9 -111 L 9 -111 L 0 -90 Z"
						className="fill-primary-text stroke-background"
						strokeWidth={1.5}
						strokeLinejoin="round"
					/>
				</svg>
			)}
			<p className="sr-only" aria-live="polite">
				{spunRef.current && announcement()}
			</p>
			{picked ? (
				<div
					ref={cardRef}
					className="pick-result flex flex-col gap-4 w-full max-w-[480px] rounded-xl border border-primary/30 bg-primary/10 p-4 scroll-mb-28 md:scroll-mb-6"
				>
					<div className="flex items-center gap-4 min-w-0">
						<div className="size-20 shrink-0 overflow-hidden rounded-lg">
							<BgtFallbackImage title={picked.title} image={picked.image} decorative />
						</div>
						<div className="flex flex-col gap-1 min-w-0">
							<span className="text-sm text-white/60">{t("pick.tonight")}</span>
							<span className="text-xl font-bold text-white">{picked.title}</span>
							<span className="text-sm text-card-value">
								{t("pick.players", { range: formatRange(picked.minPlayers, picked.maxPlayers) ?? "-" })}
								{" · "}
								{durationRange(picked.minPlayTime, picked.maxPlayTime) ?? "-"}
							</span>
						</div>
					</div>
					<div className="grid grid-cols-2 gap-2">
						<BgtButton
							data-pick-action
							className="min-h-11"
							onClick={() => navigate({ to: "/games/$gameId", params: { gameId: picked.id } })}
						>
							{t("pick.open")}
						</BgtButton>
						{canWrite && (
							<BgtButton
								variant="cancel"
								className="min-h-11"
								onClick={() => navigate({ to: "/sessions/new/$gameId", params: { gameId: picked.id } })}
							>
								{t("pick.log")}
							</BgtButton>
						)}
						{games.length > 1 && (
							<>
								<BgtButton variant="text" className="min-h-11" onClick={spinAgain}>
									{t("pick.again")}
								</BgtButton>
								<BgtButton variant="text" className="min-h-11" onClick={() => veto(picked)}>
									{t("pick.exclude")}
								</BgtButton>
							</>
						)}
					</div>
				</div>
			) : (
				<BgtButton
					data-pick-action
					aria-disabled={spinning || undefined}
					onClick={spinAgain}
					className={cx("min-h-11", spinning && "opacity-60 cursor-wait")}
				>
					{spinning ? t("pick.spinning") : t("pick.spin")}
				</BgtButton>
			)}
		</div>
	);
};
