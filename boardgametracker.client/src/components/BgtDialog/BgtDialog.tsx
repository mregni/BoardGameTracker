import * as Dialog from "@radix-ui/react-dialog";
import { cva, cx, type VariantProps } from "class-variance-authority";
import { type ComponentPropsWithoutRef, createContext, type ReactNode, useContext, useRef, useState } from "react";
import { useTranslation } from "react-i18next";

import Cross from "@/assets/icons/x.svg?react";

const DialogCloseContext = createContext<(() => void) | undefined>(undefined);
const DialogDepthContext = createContext(0);
const DialogPortalContainerContext = createContext<HTMLElement | undefined>(undefined);

export const useDialogPortalContainer = () => useContext(DialogPortalContainerContext);

const OVERLAY_BASE_Z_INDEX = 40;
const Z_INDEX_STEP = 20;

interface BgtDialogProps {
	open: boolean;
	children: ReactNode;
	onClose: () => void;
}

export const BgtDialog = (props: BgtDialogProps) => {
	const { open, children, onClose } = props;
	const depth = useContext(DialogDepthContext);

	return (
		<Dialog.Root
			open={open}
			onOpenChange={(nextOpen) => {
				if (!nextOpen) {
					onClose();
				}
			}}
		>
			<DialogDepthContext.Provider value={depth + 1}>
				<DialogCloseContext.Provider value={onClose}>{children}</DialogCloseContext.Provider>
			</DialogDepthContext.Provider>
		</Dialog.Root>
	);
};

const centered = "left-1/2 top-1/2 w-[calc(100vw-2rem)] -translate-x-1/2 -translate-y-1/2 rounded-xl";

const dialogContent = cva("fixed flex bg-dialog text-white shadow-2xl focus:outline-none", {
	variants: {
		size: {
			default: [centered, "max-w-[600px]"],
			wide: [centered, "max-w-[600px] lg:max-w-5xl"],
			sheet: [
				"inset-x-0 bottom-0 w-full rounded-t-2xl",
				"md:inset-x-auto md:bottom-auto md:left-1/2 md:top-1/2 md:w-[calc(100vw-2rem)] md:max-w-xl md:-translate-x-1/2 md:-translate-y-1/2 md:rounded-xl",
			],
		},
	},
	defaultVariants: { size: "default" },
});

type BgtDialogContentProps = ComponentPropsWithoutRef<typeof Dialog.Content> & VariantProps<typeof dialogContent>;

export const BgtDialogContent = (props: BgtDialogContentProps) => {
	const { className, children, style, size, onOpenAutoFocus, onCloseAutoFocus, onEscapeKeyDown, ...rest } = props;
	const onClose = useContext(DialogCloseContext);
	const depth = useContext(DialogDepthContext);
	const { t } = useTranslation();
	const [container, setContainer] = useState<HTMLElement>();
	const returnFocusRef = useRef<HTMLElement | null>(null);
	const overlayZIndex = OVERLAY_BASE_Z_INDEX + Math.max(0, depth - 1) * Z_INDEX_STEP;

	const handleOpenAutoFocus = (event: Event) => {
		const active = document.activeElement;
		returnFocusRef.current = active instanceof HTMLElement && active !== document.body ? active : null;
		onOpenAutoFocus?.(event);
	};

	const handleCloseAutoFocus = (event: Event) => {
		onCloseAutoFocus?.(event);
		if (event.defaultPrevented) return;
		const target = returnFocusRef.current;
		returnFocusRef.current = null;
		if (target?.isConnected) {
			event.preventDefault();
			target.focus();
		}
	};

	const handleEscapeKeyDown = (event: KeyboardEvent) => {
		if (event.target instanceof Element && event.target.closest("[data-trigger]")) {
			event.preventDefault();
			return;
		}
		onEscapeKeyDown?.(event);
	};

	return (
		<Dialog.Portal>
			<Dialog.Overlay className="fixed inset-0 bg-black/60 backdrop-blur-[2px]" style={{ zIndex: overlayZIndex }} />
			<Dialog.Content
				ref={(node) => setContainer(node ?? undefined)}
				style={{ zIndex: overlayZIndex + 10, ...style }}
				className={cx(dialogContent({ size }), className)}
				onOpenAutoFocus={handleOpenAutoFocus}
				onCloseAutoFocus={handleCloseAutoFocus}
				onEscapeKeyDown={handleEscapeKeyDown}
				{...rest}
			>
				{onClose && (
					<button
						type="button"
						onClick={onClose}
						aria-label={t("close")}
						className="absolute top-1 right-1 p-1 pointer-coarse:p-3 hover:bg-transparent rounded-lg cursor-pointer hover:scale-110 transition-transform"
					>
						<Cross className="size-5" />
					</button>
				)}
				<div className="flex max-h-[calc(100dvh-2rem)] w-full flex-col gap-3 overflow-y-auto rounded-[inherit] p-6">
					<DialogPortalContainerContext.Provider value={container}>{children}</DialogPortalContainerContext.Provider>
				</div>
			</Dialog.Content>
		</Dialog.Portal>
	);
};

type BgtDialogTitleProps = ComponentPropsWithoutRef<typeof Dialog.Title>;

export const BgtDialogTitle = (props: BgtDialogTitleProps) => {
	const { className, children, ...rest } = props;
	return (
		<Dialog.Title className={cx("text-2xl font-bold", className)} {...rest}>
			{children}
		</Dialog.Title>
	);
};

type BgtDialogDescriptionProps = ComponentPropsWithoutRef<typeof Dialog.Description>;

export const BgtDialogDescription = (props: BgtDialogDescriptionProps) => {
	const { className, children, ...rest } = props;
	return (
		<Dialog.Description className={cx("text-white/70", className)} {...rest}>
			{children}
		</Dialog.Description>
	);
};
